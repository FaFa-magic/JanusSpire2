using JanusSpire2.JanusSpire2Code.Cards.Ancient;
using JanusSpire2.JanusSpire2Code.Cards.Common;
using JanusSpire2.JanusSpire2Code.Cards.Rare;
using JanusSpire2.JanusSpire2Code.Cards;
using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Patches;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;
using JanusSpire2.JanusSpire2Code.Relics;

namespace JanusSpire2.JanusSpire2Code.Singleton;

[RegisterSingleton]
public class JanusSingleton : HookedSingletonModel
{
    private static readonly HashSet<CardModel> CardsWithDiaryPlayResult = [];
    private static readonly HashSet<CardModel> CardsProtectedFromNextDelayedFlush = [];
    private readonly Dictionary<CardModel, (int TurnNumber, int TriggerCount)> _counterattackTriggerCounts = new();
    private readonly HashSet<Player> _playersRetainingDelayedHand = [];

    public static JanusSingleton? Instance { get; private set; }

    public JanusSingleton() : base(HookType.Combat)
    {
    }

    internal static async Task AutoPlayWithDiaryResult(
        PlayerChoiceContext choiceContext,
        CardModel card,
        Creature? target = null)
    {
        bool addedMarker = CardsWithDiaryPlayResult.Add(card);
        try
        {
            await CardCmd.AutoPlay(choiceContext, card, target);
        }
        finally
        {
            if (addedMarker)
            {
                CardsWithDiaryPlayResult.Remove(card);
            }
        }
    }

    internal static async Task AutoPlayGeneratedCardAndExhaust(
        PlayerChoiceContext choiceContext,
        CardModel card)
    {
        card.ExhaustOnNextPlay = true;
        await CardCmd.AutoPlay(choiceContext, card, null);

        if (CombatManager.Instance.IsOverOrEnding || card.Owner.Creature.IsDead ||
            card.Pile is not { } pile || !pile.Type.IsCombatPile() ||
            pile.Type == PileType.Exhaust ||
            CombatManager.Instance.History.Entries.OfType<CardExhaustedEntry>()
                .Any(entry => ReferenceEquals(entry.Card, card)))
        {
            return;
        }

        await CardCmd.Exhaust(choiceContext, card);
    }

    internal static bool HasDiaryPlayResult(CardModel card) => CardsWithDiaryPlayResult.Contains(card);

    internal static void ProtectFromNextDelayedFlush(CardModel card)
    {
        CardsProtectedFromNextDelayedFlush.Add(card);
    }

    public override async Task BeforeCombatStart()
    {
        CardsWithDiaryPlayResult.Clear();
        CardsProtectedFromNextDelayedFlush.Clear();
        _counterattackTriggerCounts.Clear();
        _playersRetainingDelayedHand.Clear();
        foreach (Player player in CurrentCombatState?.Players ?? [])
        {
            await RecordExtraHandManager.SyncPlayer(player);
        }
    }

    public override Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || CurrentCombatState == null)
        {
            return Task.CompletedTask;
        }

        HashSet<Creature> participantSet = participants.ToHashSet();
        foreach (Player player in CurrentCombatState.Players)
        {
            if (!participantSet.Contains(player.Creature) ||
                player.Relics.All(relic => relic is not ISkipPlayerFlushRelic))
            {
                continue;
            }

            if (player.Creature.HasPower<RetainHandPower>())
            {
                _playersRetainingDelayedHand.Add(player);
            }
            else
            {
                _playersRetainingDelayedHand.Remove(player);
            }
        }

        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEndLate(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy || CurrentCombatState == null)
        {
            return;
        }

        foreach (Player player in CurrentCombatState.Players)
        {
            bool retainDelayedHand = _playersRetainingDelayedHand.Remove(player);
            if (player.Relics.All(relic => relic is not ISkipPlayerFlushRelic))
            {
                continue;
            }

            await FlushDelayedHand(
                choiceContext,
                CurrentCombatState,
                player,
                retainDelayedHand);
        }
    }

    private static async Task FlushDelayedHand(
        PlayerChoiceContext choiceContext,
        ICombatState combatState,
        Player player,
        bool retainHand)
    {
        if (player.Creature.IsDead || player.PlayerCombatState == null)
        {
            return;
        }

        CardPile hand = PileType.Hand.GetPile(player);
        List<CardModel> cardsToFlush = [];
        List<CardModel> cardsToRetain = [];
        bool shouldFlush = !retainHand && Hook.ShouldFlush(combatState, player);

        foreach (CardModel card in hand.Cards.ToArray())
        {
            bool protectedByCentennialPuzzle =
                CardsProtectedFromNextDelayedFlush.Remove(card);
            if (!shouldFlush || card.ShouldRetainThisTurn || protectedByCentennialPuzzle)
            {
                cardsToRetain.Add(card);
            }
            else
            {
                cardsToFlush.Add(card);
            }
        }

        List<CardModel> flushedCards = [];
        foreach (CardModel card in cardsToFlush)
        {
            if (!ReferenceEquals(card.Pile, hand))
            {
                continue;
            }

            CardPileAddResult result = await CardPileCmd.Add(card, PileType.Discard);
            if (result.success)
            {
                flushedCards.Add(card);
            }
        }

        await Hook.AfterFlush(
            combatState,
            player,
            choiceContext,
            flushedCards,
            cardsToRetain);
    }

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        if (oldPileType == PileType.Hand && card.Pile?.Type != PileType.Hand)
        {
            CardsProtectedFromNextDelayedFlush.Remove(card);
        }

        if (card is not JanusRecordMappingCard && card.Owner?.PlayerCombatState != null)
        {
            await RecordExtraHandManager.SyncPlayer(card.Owner);
        }
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        Player? player = target.Player;
        if (player == null || target != player.Creature)
        {
            return;
        }

        await TriggerCounterattacks(choiceContext, player);
    }

    private async Task TriggerCounterattacks(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        PlayerCombatState? playerCombatState = player.PlayerCombatState;
        if (playerCombatState == null ||
            CombatManager.Instance.IsOverOrEnding ||
            player.Creature.IsDead)
        {
            return;
        }

        List<CardModel> counterattackCards = PileType.Hand.GetPile(player).Cards
            .Where(card => card.Keywords.Contains(JanusKeywords.Counterattack))
            .ToList();

        // Black Cat Unleash can also counterattack outside the hand, but never while
        // exhausted, playing, or unpiled. Resolve these sources after the hand.
        counterattackCards.AddRange(playerCombatState.AllCards.Where(card =>
            card is BlackCatUnleash &&
            card.Pile != null &&
            card.Pile.Type != PileType.Hand &&
            card.Pile.Type != PileType.Exhaust &&
            card.Pile.Type != PileType.Play &&
            card.Pile.Type != PileType.None &&
            card.Keywords.Contains(JanusKeywords.Counterattack) &&
            !counterattackCards.Contains(card)));

        foreach (CardModel card in counterattackCards)
        {
            // Earlier counterattacks can move another candidate during their hooks
            // or choices. Preserve snapshot order, but recheck current eligibility.
            PileType? sourcePile = card.Pile?.Type;
            if (CombatManager.Instance.IsOverOrEnding || player.Creature.IsDead)
                break;
            if (sourcePile == null || sourcePile == PileType.None || sourcePile == PileType.Play ||
                sourcePile == PileType.Exhaust || card.Owner != player ||
                !card.Keywords.Contains(JanusKeywords.Counterattack) ||
                !(sourcePile == PileType.Hand || card is BlackCatUnleash))
                continue;

            int triggerLimit = card.EnergyCost.CostsX ? 1 : card.EnergyCost.GetAmountToSpend();
            int turnNumber = playerCombatState.TurnNumber;
            (int TurnNumber, int TriggerCount) triggerState =
                _counterattackTriggerCounts.GetValueOrDefault(card);
            int triggerCount = triggerState.TurnNumber == turnNumber
                ? triggerState.TriggerCount
                : 0;
            if (triggerCount >= triggerLimit)
            {
                continue;
            }

            _counterattackTriggerCounts[card] = (turnNumber, triggerCount + 1);
            CardModel copy = card.CreateClone();
            copy.ExhaustOnNextPlay = true;
            CardPileAddResult addResult = await CardPileCmd.AddGeneratedCardToCombat(
                copy,
                PileType.Play,
                player);
            if (addResult.success)
                await AutoPlayGeneratedCardAndExhaust(choiceContext, addResult.cardAdded);

            if (CombatManager.Instance.IsOverOrEnding || player.Creature.IsDead)
            {
                break;
            }
        }
    }

    public override Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal)
    {
        if (CombatManager.Instance.IsOverOrEnding ||
            card.Pile?.Type != PileType.Exhaust ||
            !card.Keywords.Contains(JanusKeywords.Sticker) ||
            !card.IsUpgradable)
        {
            return Task.CompletedTask;
        }

        return StickerMergeAction.Merge(card);
    }

    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        CardLocation cardLocation)
    {
        // Return-to-hand effects (e.g. Feral) take priority over Diary redirects.
        // Preserve their recipient and position as well as the destination pile.
        if (cardLocation.pileType == PileType.Hand)
        {
            return cardLocation;
        }

        if (CardsWithDiaryPlayResult.Contains(card) &&
            cardLocation.pileType.IsCombatPile() &&
            cardLocation.pileType != PileType.Play)
        {
            cardLocation.pileType = MainFile.Diary;
            return cardLocation;
        }

        if (cardLocation.pileType == PileType.Discard &&
            card.Keywords.Contains(JanusKeywords.Record))
        {
            cardLocation.pileType = MainFile.Diary;
        }

        return cardLocation;
    }
}

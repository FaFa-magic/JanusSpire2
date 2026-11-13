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
using STS2RitsuLib.Utils;
using JanusSpire2.JanusSpire2Code.Relics;

namespace JanusSpire2.JanusSpire2Code.Singleton;

[RegisterSingleton]
public class JanusSingleton : HookedSingletonModel
{
    private static readonly AttachedState<ICombatState, CombatData> CombatStates = new(static () => new());

    private sealed class CombatData
    {
        public HashSet<CardModel> DiaryPlayResults { get; } = [];
        public HashSet<CardModel> ProtectedFromDelayedFlush { get; } = [];
        public Dictionary<CardModel, (int TurnNumber, int TriggerCount)> CounterattackCounts { get; } = [];
        public HashSet<Player> PlayersRetainingDelayedHand { get; } = [];
    }

    public static JanusSingleton? Instance { get; private set; }

    public JanusSingleton() : base(HookType.Combat)
    {
    }

    internal static async Task AutoPlayWithDiaryResult(
        PlayerChoiceContext choiceContext,
        CardModel card,
        Creature? target = null)
    {
        ICombatState? combatState = card.Owner?.Creature.CombatState;
        CombatData? data = combatState != null ? CombatStates.GetOrCreate(combatState) : null;
        bool addedMarker = data?.DiaryPlayResults.Add(card) == true;
        try
        {
            await CardCmd.AutoPlay(choiceContext, card, target);
        }
        finally
        {
            if (addedMarker && data != null)
            {
                data.DiaryPlayResults.Remove(card);
            }
        }
    }

    internal static async Task AutoPlayGeneratedCardAndExhaust(
        PlayerChoiceContext choiceContext,
        CardModel card)
    {
        ICombatState? combatState = card.Owner?.Creature.CombatState;
        card.ExhaustOnNextPlay = true;
        await CardCmd.AutoPlay(choiceContext, card, null);

        if (combatState == null || !ReferenceEquals(card.Owner?.Creature.CombatState, combatState) ||
            CombatManager.Instance.IsOverOrEnding || card.Owner.Creature.IsDead ||
            card.Pile is not { } pile || !pile.Type.IsCombatPile() ||
            pile.Type == PileType.Exhaust ||
            CombatManager.Instance.History.Entries.OfType<CardExhaustedEntry>()
                .Any(entry => ReferenceEquals(entry.Card, card)))
        {
            return;
        }

        await CardCmd.Exhaust(choiceContext, card);
    }

    internal static bool HasDiaryPlayResult(CardModel card) =>
        card.Owner?.Creature.CombatState is { } combatState &&
        CombatStates.TryGetValue(combatState, out CombatData? data) &&
        data.DiaryPlayResults.Contains(card);

    internal static void ProtectFromNextDelayedFlush(ICombatState combatState, CardModel card)
    {
        if (card.Pile?.Type == PileType.Hand && ReferenceEquals(card.Owner.Creature.CombatState, combatState))
        {
            CombatStates.GetOrCreate(combatState).ProtectedFromDelayedFlush.Add(card);
        }
    }

    public override async Task BeforeCombatStart()
    {
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
        if (side != CombatSide.Player || CurrentCombatState is not { } combatState)
        {
            return Task.CompletedTask;
        }

        HashSet<Creature> participantSet = participants.ToHashSet();
        foreach (Player player in combatState.Players)
        {
            if (!participantSet.Contains(player.Creature) ||
                player.Relics.All(relic => relic is not ISkipPlayerFlushRelic))
            {
                continue;
            }

            if (player.Creature.HasPower<RetainHandPower>())
            {
                CombatStates.GetOrCreate(combatState).PlayersRetainingDelayedHand.Add(player);
            }
            else if (CombatStates.TryGetValue(combatState, out CombatData? data))
            {
                data.PlayersRetainingDelayedHand.Remove(player);
            }
        }

        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEndLate(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy || CurrentCombatState is not { } combatState)
        {
            return;
        }

        CombatData data = CombatStates.GetOrCreate(combatState);
        foreach (Player player in combatState.Players)
        {
            if (!ReferenceEquals(CurrentCombatState, combatState))
            {
                return;
            }

            bool retainDelayedHand = data.PlayersRetainingDelayedHand.Remove(player);
            if (player.Relics.All(relic => relic is not ISkipPlayerFlushRelic))
            {
                continue;
            }

            await FlushDelayedHand(
                choiceContext,
                combatState,
                player,
                retainDelayedHand,
                data.ProtectedFromDelayedFlush);
        }
    }

    private static async Task FlushDelayedHand(
        PlayerChoiceContext choiceContext,
        ICombatState combatState,
        Player player,
        bool retainHand,
        HashSet<CardModel> protectedCards)
    {
        if (!ReferenceEquals(player.Creature.CombatState, combatState) ||
            player.Creature.IsDead || player.PlayerCombatState == null)
        {
            return;
        }

        CardPile hand = PileType.Hand.GetPile(player);
        List<CardModel> cardsToFlush = [];
        List<CardModel> cardsToRetain = [];
        bool shouldFlush = !retainHand && Hook.ShouldFlush(combatState, player);

        foreach (CardModel card in hand.Cards.ToArray())
        {
            bool protectedByCentennialPuzzle = protectedCards.Remove(card);
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
            if (!ReferenceEquals(player.Creature.CombatState, combatState))
            {
                return;
            }

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

        if (ReferenceEquals(player.Creature.CombatState, combatState))
        {
            await Hook.AfterFlush(
                combatState,
                player,
                choiceContext,
                flushedCards,
                cardsToRetain);
        }
    }

    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        if (oldPileType == PileType.Hand && card.Pile?.Type != PileType.Hand &&
            CurrentCombatState is { } combatState &&
            CombatStates.TryGetValue(combatState, out CombatData? data))
        {
            data.ProtectedFromDelayedFlush.Remove(card);
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

    private static async Task TriggerCounterattacks(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        PlayerCombatState? playerCombatState = player.PlayerCombatState;
        if (playerCombatState == null || player.Creature.CombatState is not { } combatState ||
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

        if (counterattackCards.Count == 0)
        {
            return;
        }

        var counts = CombatStates.GetOrCreate(combatState).CounterattackCounts;
        foreach (CardModel card in counterattackCards)
        {
            // Earlier counterattacks can move another candidate during their hooks
            // or choices. Preserve snapshot order, but recheck current eligibility.
            PileType? sourcePile = card.Pile?.Type;
            if (!ReferenceEquals(player.Creature.CombatState, combatState) ||
                !ReferenceEquals(player.PlayerCombatState, playerCombatState) ||
                CombatManager.Instance.IsOverOrEnding || player.Creature.IsDead)
                break;
            if (sourcePile == null || sourcePile == PileType.None || sourcePile == PileType.Play ||
                sourcePile == PileType.Exhaust || card.Owner != player ||
                !card.Keywords.Contains(JanusKeywords.Counterattack) ||
                !(sourcePile == PileType.Hand || card is BlackCatUnleash))
                continue;

            int triggerLimit = card.EnergyCost.CostsX ? 1 : card.EnergyCost.GetAmountToSpend();
            int turnNumber = playerCombatState.TurnNumber;
            (int TurnNumber, int TriggerCount) triggerState =
                counts.GetValueOrDefault(card);
            int triggerCount = triggerState.TurnNumber == turnNumber
                ? triggerState.TriggerCount
                : 0;
            if (triggerCount >= triggerLimit)
            {
                continue;
            }

            counts[card] = (turnNumber, triggerCount + 1);
            CardModel copy = card.CreateClone();
            copy.ExhaustOnNextPlay = true;
            CardPileAddResult addResult = await CardPileCmd.AddGeneratedCardToCombat(
                copy,
                PileType.Play,
                player);
            if (addResult.success &&
                ReferenceEquals(player.Creature.CombatState, combatState) &&
                ReferenceEquals(player.PlayerCombatState, playerCombatState))
            {
                await AutoPlayGeneratedCardAndExhaust(choiceContext, addResult.cardAdded);
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

        if (HasDiaryPlayResult(card) &&
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

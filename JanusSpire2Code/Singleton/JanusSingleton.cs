using JanusSpire2.JanusSpire2Code.Cards.Ancient;
using JanusSpire2.JanusSpire2Code.Cards.Common;
using JanusSpire2.JanusSpire2Code.Cards.Rare;
using JanusSpire2.JanusSpire2Code.Cards;
using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Patches;
using MegaCrit.Sts2.Core.Combat;
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
    private readonly Dictionary<CardModel, (int TurnNumber, int TriggerCount)> _counterattackTriggerCounts = new();
    private readonly HashSet<Player> _playersRetainingDelayedHand = [];

    public static JanusSingleton? Instance { get; private set; }

    public JanusSingleton() : base(HookType.Combat)
    {
    }

    public override async Task BeforeCombatStart()
    {
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
            if (!shouldFlush || card.ShouldRetainThisTurn)
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

        // Black Cat Unleash is the sole exception that can counterattack outside the hand,
        // but cards being played, exhausted, or outside every pile are not active sources.
        // Resolve valid exceptions after the hand to preserve left-to-right hand ordering.
        counterattackCards.AddRange(playerCombatState.AllCards.Where(card =>
            card is BlackCatUnleash &&
            card.Pile != null &&
            card.Pile.Type != PileType.Hand &&
            card.Pile.Type != PileType.Exhaust &&
            card.Pile.Type != PileType.Play &&
            card.Pile.Type != PileType.None &&
            card.Keywords.Contains(JanusKeywords.Counterattack)));

        foreach (CardModel card in counterattackCards)
        {
            int currentCost = card.EnergyCost.GetAmountToSpend();
            int turnNumber = playerCombatState.TurnNumber;
            (int TurnNumber, int TriggerCount) triggerState =
                _counterattackTriggerCounts.GetValueOrDefault(card);
            int triggerCount = triggerState.TurnNumber == turnNumber
                ? triggerState.TriggerCount
                : 0;
            if (triggerCount >= currentCost)
            {
                continue;
            }

            _counterattackTriggerCounts[card] = (turnNumber, triggerCount + 1);
            CardModel copy = card.CreateClone();
            copy.ExhaustOnNextPlay = true;
            await CardPileCmd.AddGeneratedCardToCombat(
                copy,
                PileType.Play,
                player);
            await CardCmd.AutoPlay(choiceContext, copy, null);

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

        StickerMergeAction.Request(card);
        return Task.CompletedTask;
    }

    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        CardLocation cardLocation)
    {
        if (Gleanings.ShouldReturnToDiary(card) || NotAfraid.ShouldReturnToDiary(card))
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

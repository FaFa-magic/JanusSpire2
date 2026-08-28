using JanusSpire2.JanusSpire2Code.Cards.Ancient;
using JanusSpire2.JanusSpire2Code.Cards.Common;
using JanusSpire2.JanusSpire2Code.Cards.Rare;
using JanusSpire2.JanusSpire2Code.Cards;
using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Patches;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
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
    private readonly List<AttackCommand> _activeAttackCommands = [];

    public static JanusSingleton? Instance { get; private set; }

    public JanusSingleton() : base(HookType.Combat)
    {
    }

    public override async Task BeforeCombatStart()
    {
        _counterattackTriggerCounts.Clear();
        _activeAttackCommands.Clear();
        foreach (Player player in CurrentCombatState?.Players ?? [])
        {
            await RecordExtraHandManager.SyncPlayer(player);
        }
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
            if (player.Relics.All(relic => relic is not ISkipPlayerFlushRelic))
            {
                continue;
            }

            await FlushDelayedHand(choiceContext, CurrentCombatState, player);
        }
    }

    private static async Task FlushDelayedHand(
        PlayerChoiceContext choiceContext,
        ICombatState combatState,
        Player player)
    {
        if (player.Creature.IsDead || player.PlayerCombatState == null)
        {
            return;
        }

        CardPile hand = PileType.Hand.GetPile(player);
        List<CardModel> cardsToFlush = [];
        List<CardModel> cardsToRetain = [];
        bool shouldFlush = Hook.ShouldFlush(combatState, player);

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

    public override Task BeforeAttack(AttackCommand command)
    {
        _activeAttackCommands.Add(command);
        return Task.CompletedTask;
    }

    public override async Task AfterAttack(
        PlayerChoiceContext choiceContext,
        AttackCommand command)
    {
        _activeAttackCommands.Remove(command);
        if (CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        List<Player> damagedPlayers = command.Results
            .SelectMany(hit => hit)
            .Select(result => result.Receiver.Player)
            .OfType<Player>()
            .Distinct()
            .ToList();
        foreach (Player player in damagedPlayers)
        {
            await TriggerCounterattacks(choiceContext, player);
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
        if (IsDamageFromActiveAttack(dealer, props, cardSource))
        {
            return;
        }

        Player? player = target.Player;
        if (player == null || target != player.Creature)
        {
            return;
        }

        await TriggerCounterattacks(choiceContext, player);
    }

    private bool IsDamageFromActiveAttack(
        Creature? dealer,
        ValueProp props,
        CardModel? cardSource)
    {
        return _activeAttackCommands.Any(command =>
            command.Attacker == dealer &&
            command.DamageProps == props &&
            command.ModelSource as CardModel == cardSource);
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

        List<CardModel> counterattackCards = playerCombatState.AllCards
            .Where(card =>
                card.Keywords.Contains(JanusKeywords.Counterattack) &&
                (card.Pile?.Type == PileType.Hand || card is BlackCatUnleash))
            .Select((card, index) => new
            {
                Card = card,
                Index = index,
                Cost = card.EnergyCost.GetAmountToSpend()
            })
            .OrderBy(entry => entry.Cost)
            .ThenBy(entry => entry.Index)
            .Select(entry => entry.Card)
            .ToList();

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
            CardModel cardclone = card.CreateClone();
            cardclone.ExhaustOnNextPlay = true;
            await CardCmd.AutoPlay(choiceContext, cardclone, null);

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

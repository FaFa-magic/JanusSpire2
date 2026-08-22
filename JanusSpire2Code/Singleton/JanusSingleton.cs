using JanusSpire2.JanusSpire2Code.Cards.Ancient;
using JanusSpire2.JanusSpire2Code.Cards.Common;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace JanusSpire2.JanusSpire2Code.Singleton;

[RegisterSingleton]
public class JanusSingleton : HookedSingletonModel
{
    private readonly Dictionary<CardModel, (int TurnNumber, int TriggerCount)> _counterattackTriggerCounts = new();

    public static JanusSingleton? Instance { get; private set; }

    public JanusSingleton() : base(HookType.Combat)
    {
    }

    public override Task BeforeCombatStart()
    {
        _counterattackTriggerCounts.Clear();
        return Task.CompletedTask;
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
        PlayerCombatState? playerCombatState = player?.PlayerCombatState;
        if (player == null || playerCombatState == null ||
            target != player.Creature || CombatManager.Instance.IsOverOrEnding)
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

    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        CardLocation cardLocation)
    {
        if (Gleanings.ShouldReturnToDiary(card))
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

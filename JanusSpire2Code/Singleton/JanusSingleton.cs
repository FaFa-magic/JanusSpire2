using JanusSpire2.JanusSpire2Code.Cards.Ancient;
using JanusSpire2.JanusSpire2Code.Cards.Common;
using JanusSpire2.JanusSpire2Code.Cards.Rare;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace JanusSpire2.JanusSpire2Code.Singleton;

[RegisterSingleton]
public class JanusSingleton : HookedSingletonModel
{
    private const int StickerMergeCount = 4;

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

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal)
    {
        if (CombatManager.Instance.IsOverOrEnding ||
            card.Pile?.Type != PileType.Exhaust ||
            !card.Keywords.Contains(JanusKeywords.Sticker) ||
            !card.IsUpgradable)
        {
            return;
        }

        List<CardModel> matchingStickers = PileType.Exhaust.GetPile(card.Owner).Cards
            .Where(candidate =>
                candidate.Id == card.Id &&
                candidate.CurrentUpgradeLevel == card.CurrentUpgradeLevel &&
                candidate.Keywords.Contains(JanusKeywords.Sticker))
            .Take(StickerMergeCount)
            .ToList();

        if (matchingStickers.Count < StickerMergeCount)
        {
            return;
        }

        ICombatState? combatState = card.CombatState;
        if (combatState == null)
        {
            return;
        }

        CardModel upgradedSticker = combatState.CreateCard(ModelDb.GetById<CardModel>(card.Id), card.Owner);
        for (int i = 0; i <= card.CurrentUpgradeLevel; i++)
        {
            CardCmd.Upgrade(upgradedSticker, CardPreviewStyle.None);
        }

        await CardPileCmd.RemoveFromCombat(matchingStickers);
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(
            upgradedSticker,
            MainFile.Diary,
            card.Owner);
        CardCmd.PreviewCardPileAdd(result, 0.2F);
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

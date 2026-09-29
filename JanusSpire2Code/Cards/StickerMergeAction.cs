using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace JanusSpire2.JanusSpire2Code.Cards;

internal static class StickerMergeAction
{
    private const int RequiredCopies = 4;

    // This automatic effect belongs to the synchronized exhaust command, not a
    // second network action that may interleave with its caller's continuation.
    internal static async Task Merge(CardModel trigger)
    {
        var owner = trigger.Owner;
        ICombatState? combatState = owner.Creature.CombatState;
        if (combatState == null || trigger.Pile?.Type != PileType.Exhaust ||
            !trigger.Keywords.Contains(JanusKeywords.Sticker) || !trigger.IsUpgradable)
            return;

        ModelId cardId = trigger.Id;
        int upgradeLevel = trigger.CurrentUpgradeLevel;
        while (CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsOverOrEnding &&
               ReferenceEquals(owner.Creature.CombatState, combatState))
        {
            List<CardModel> matching = PileType.Exhaust.GetPile(owner).Cards
                .Where(card => card.Id == cardId && card.CurrentUpgradeLevel == upgradeLevel &&
                               card.Keywords.Contains(JanusKeywords.Sticker) && card.IsUpgradable)
                .Take(RequiredCopies).ToList();
            if (matching.Count < RequiredCopies)
                return;

            CardModel canonical = ModelDb.GetById<CardModel>(cardId);
            CardModel upgradedSticker = combatState.CreateCard(canonical, owner);
            for (int i = 0; i <= upgradeLevel; i++)
                CardCmd.Upgrade(upgradedSticker, CardPreviewStyle.None);

            await CardPileCmd.RemoveFromCombat(matching);
            if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsOverOrEnding ||
                !ReferenceEquals(owner.Creature.CombatState, combatState))
                return;

            CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(
                upgradedSticker, MainFile.Diary, owner);
            CardCmd.PreviewCardPileAdd(result, 0.2F);
        }
    }
}

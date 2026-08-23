using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class SealedTreasureChest : JanusRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(5)
    ];

    public override async Task AfterObtained()
    {
        List<CardCreationResult> candidates = Owner.Character.CardPool
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .DistinctBy(card => card.Id)
            .Select(card => new CardCreationResult(Owner.RunState.CreateCard(card, Owner)))
            .ToList();

        int selectionCount = Math.Min(DynamicVars.Cards.IntValue, candidates.Count);
        if (selectionCount <= 0)
        {
            return;
        }

        CardSelectorPrefs prefs = new(SelectionScreenPrompt, selectionCount)
        {
            Cancelable = false,
            RequireManualConfirmation = true
        };
        List<CardModel> selectedCards = (await CardSelectCmd.FromSimpleGridForRewards(
            new BlockingPlayerChoiceContext(),
            candidates,
            Owner,
            prefs)).ToList();

        if (selectedCards.Count == 0)
        {
            return;
        }

        Flash();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.Add(selectedCards, PileType.Deck),
            1.2f,
            CardPreviewStyle.GridLayout);
    }

    public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room is not CombatRoom)
        {
            return false;
        }

        bool removedCardReward = rewards.RemoveAll(reward => reward is CardReward) > 0;
        if (removedCardReward)
        {
            Flash();
        }

        return removedCardReward;
    }
}

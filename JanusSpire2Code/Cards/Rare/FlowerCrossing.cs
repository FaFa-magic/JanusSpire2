using MegaCrit.Sts2.Core.Entities.Cards;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class FlowerCrossing() : JanusCardModel(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int amount = ResolveEnergyXValue();
        if (amount <= 0)
        {
            return;
        }

        List<CardModel> generatedCards = CardFactory.GetForCombat(
            Owner,
            Owner.Character.CardPool.GetUnlockedCards(
                Owner.UnlockState,
                Owner.RunState.CardMultiplayerConstraint),
            amount,
            Owner.RunState.Rng.CombatCardGeneration).ToList();

        foreach (CardModel generatedCard in generatedCards)
        {
            generatedCard.AddKeyword(JanusKeywords.Record);
            generatedCard.SetToFreeThisTurn();
            if (IsUpgraded)
            {
                CardCmd.Upgrade(generatedCard);
            }
        }

        await CardPileCmd.AddGeneratedCardsToCombat(generatedCards, PileType.Hand, Owner);
    }
}
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class ChangingTime() : JanusCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> generatedCards = CardFactory.GetForCombat(
            Owner,
            Owner.Character.CardPool.GetUnlockedCards(
                Owner.UnlockState,
                Owner.RunState.CardMultiplayerConstraint),
            DynamicVars.Cards.IntValue,
            Owner.RunState.Rng.CombatCardGeneration).ToList();

        if (generatedCards.Count > 0)
        {
            await CardPileCmd.AddGeneratedCardsToCombat(generatedCards, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1M);
}

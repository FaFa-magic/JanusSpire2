using JanusSpire2.JanusSpire2Code.Singleton;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class NotAfraid() : JanusCardModel(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(4)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<CardModel> candidates = Owner.UnlockState.CharacterCardPools
            .SelectMany(pool => pool.GetUnlockedCards(
                Owner.UnlockState,
                Owner.RunState.CardMultiplayerConstraint));
        List<CardModel> cardsToPlay = CardFactory.GetForCombat(
            Owner,
            candidates,
            DynamicVars.Cards.IntValue,
            Owner.RunState.Rng.CombatCardGeneration).ToList();
        if (cardsToPlay.Count == 0)
        {
            return;
        }
        
        await CardPileCmd.AddGeneratedCardsToCombat(cardsToPlay, PileType.Play, Owner);

        foreach (CardModel card in cardsToPlay)
        {
            if (CombatManager.Instance.IsOverOrEnding || Owner.Creature.IsDead)
            {
                break;
            }

            await JanusSingleton.AutoPlayWithDiaryResult(choiceContext, card);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(2M);
}

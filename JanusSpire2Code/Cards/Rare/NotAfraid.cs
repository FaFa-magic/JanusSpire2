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
    private static readonly HashSet<CardModel> CardsReturningToDiary = [];

    internal static bool ShouldReturnToDiary(CardModel card) =>
        CardsReturningToDiary.Contains(card);

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

            CardsReturningToDiary.Add(card);
            try
            {
                await CardCmd.AutoPlay(choiceContext, card, null);

                // Unplayable or hook-blocked cards bypass the normal result-location hook.
                if (!card.HasBeenRemovedFromState &&
                    card.Pile?.IsCombatPile == true &&
                    card.Pile.Type != MainFile.Diary &&
                    !CombatManager.Instance.IsOverOrEnding &&
                    !Owner.Creature.IsDead)
                {
                    await CardPileCmd.Add(card, MainFile.Diary);
                }
            }
            finally
            {
                CardsReturningToDiary.Remove(card);
            }
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(2M);
}

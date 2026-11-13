using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.Transforms;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class SecretarialWork() : JanusCardModel(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    internal static void RegisterTransformation()
    {
        // Vanilla determines the result pile before OnPlay, when the selection may still be empty.
        // Move only the replacement from this play after vanilla has updated its table node.
        ModCardTransformRegistry.For(MainFile.ModId).Register(
            "secretarial_work_to_hand",
            context => context.Original is SecretarialWork original &&
                       context.OriginalPile.Type == PileType.Play &&
                       context.Replacement.Pile?.Type == PileType.Play &&
                       ReferenceEquals(original.TransformIntoAfterPlay, context.Replacement),
            async context =>
            {
                await CardPileCmd.Add(context.Replacement, PileType.Hand);
            });
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!cardPlay.IsLastInSeries || Pile?.Type != PileType.Play || CombatState is not { } combatState)
        {
            return;
        }

        CardModel? cardModel = (await CardSelectCmd.FromCombatPile(
            prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
            context: choiceContext,
            pile: PileType.Draw.GetPile(Owner),
            player: Owner,
            filter: card => card is not SecretarialWork &&
                            (card.Type is CardType.Skill or CardType.Attack))).FirstOrDefault();
        if (cardModel != null && ReferenceEquals(CombatState, combatState) && Pile?.Type == PileType.Play)
        {
            CardModel cardClone = cardModel.CreateClone();
            cardClone.SetToFreeThisCombat();
            TransformIntoAfterPlay = cardClone;
        }
    }
    
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}

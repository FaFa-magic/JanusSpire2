using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class SecretarialWork() : JanusCardModel(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Pile?.Type != PileType.Play)
        {
            return;
        }

        CardModel? cardModel = (await CardSelectCmd.FromCombatPile(
            prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
            context: choiceContext,
            pile: PileType.Draw.GetPile(Owner),
            player: Owner,
            filter: card => card.Type is CardType.Skill or CardType.Attack)).FirstOrDefault();
        if (cardModel != null)
        {
            CardModel cardClone = cardModel.CreateClone();
            cardClone.SetToFreeThisCombat();
            CardPileAddResult? transformResult = await CardCmd.Transform(this, cardClone);
            if (transformResult is { } result && result.cardAdded != null)
            {
                await CardPileCmd.Add(result.cardAdded, PileType.Hand);
            }
        }
    }
    
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
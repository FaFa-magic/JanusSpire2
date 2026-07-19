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
        CardModel? cardModel = (await CardSelectCmd.FromCombatPile(prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1), context: choiceContext, pile: PileType.Draw.GetPile(base.Owner), player: base.Owner, filter: (CardModel c) => c.Type == CardType.Skill || c.Type == CardType.Attack)).FirstOrDefault();
        if (cardModel != null)
        {
            CardModel cardClone = cardModel.CreateClone();
            cardClone.SetToFreeThisCombat();
            await CardCmd.Transform(this, cardClone);
        }
    }
    
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
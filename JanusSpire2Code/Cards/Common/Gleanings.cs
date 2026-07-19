using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Common;

public sealed class Gleanings() : JanusCardModel(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? cardModel = PileType.Draw.GetPile(base.Owner).Cards.FirstOrDefault();
        if (cardModel != null)
        {
            await CardCmd.AutoPlay(choiceContext, cardModel, null);
            await CardPileCmd.Add(cardModel, MainFile.Diary);
        }
    }
    
    protected override void OnUpgrade() => base.EnergyCost.UpgradeBy(-1);
}
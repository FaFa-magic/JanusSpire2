using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Common;

public sealed class Gleanings() : JanusCardModel(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var drawPile = PileType.Draw.GetPile(base.Owner);
        var cardsToLookAt = drawPile.Cards.Take(DynamicVars.Cards.IntValue).ToList();
        
        if (cardsToLookAt.Count == 0)
        {
            return;
        }

        var prefs = new CardSelectorPrefs(base.SelectionScreenPrompt, 1);
        var selected = (await CardSelectCmd.FromSimpleGrid(choiceContext, cardsToLookAt, base.Owner, prefs)).FirstOrDefault();

        if (selected != null)
        {
            await CardCmd.AutoPlay(choiceContext, selected, null);
            await CardPileCmd.Add(selected, MainFile.Diary);
        }
    }
    
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1M);
}
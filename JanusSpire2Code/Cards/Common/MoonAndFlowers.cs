using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Common;

public sealed class MoonAndFlowers() : JanusCardModel(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> selectedCards = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, DynamicVars.Cards.IntValue),
            null,
            this)).ToList();

        IReadOnlyList<CardPileAddResult> results = await CardPileCmd.Add(selectedCards, MainFile.Diary);
        int movedCards = results.Count(result => result.success);
        if (movedCards > 0)
        {
            await CardPileCmd.Draw(choiceContext, movedCards, Owner);
        }
    }
    
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1M);
}

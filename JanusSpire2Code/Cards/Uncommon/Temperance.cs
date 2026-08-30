using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using JanusSpire2.JanusSpire2Code.Cards.Token;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class Temperance() : JanusCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(3)
    ];
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Perk];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromCard<CatSticker>(IsUpgraded)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> selectedCards = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, DynamicVars.Cards.IntValue),
            null,
            this)).ToList();

        IReadOnlyList<CardPileAddResult> addResults = await CardPileCmd.Add(selectedCards, MainFile.Diary);
        int cardsAdded = addResults.Count(result => result.success);
        if (cardsAdded == 0)
        {
            return;
        }

        await CatSticker.CreateInHand(
            Owner,
            cardsAdded,
            CombatState,
            IsUpgraded);
    }
}

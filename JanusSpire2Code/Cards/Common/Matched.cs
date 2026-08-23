using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Common;

public sealed class Matched() : JanusCardModel(2, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [JanusKeywords.Counterattack];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardSelectorPrefs prefs = new(SelectionScreenPrompt, DynamicVars.Cards.IntValue)
        {
            PretendCardsCanBePlayed = true
        };

        List<CardModel> selectedCards = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            prefs,
            card => !card.Keywords.Contains(CardKeyword.Unplayable),
            this)).ToList();

        foreach (CardModel selectedCard in selectedCards)
        {
            if (IsUpgraded && selectedCard.IsUpgradable)
            {
                CardCmd.Upgrade(selectedCard);
            }

            await CardCmd.AutoPlay(choiceContext, selectedCard, null);
        }
    }
}

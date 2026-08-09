using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace JanusSpire2.JanusSpire2Code.Cards.Token;

[RegisterCard(typeof(TokenCardPool))]
public sealed class TeaPartyTime() : JanusRecordCardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(3)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardSelectorPrefs prefs = new CardSelectorPrefs(base.SelectionScreenPrompt, 0, DynamicVars.Cards.IntValue);

        IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            base.Owner,
            prefs,
            card => card.Type == CardType.Status || card.Rarity == CardRarity.Curse,
            this
        );

        List<CardModel> selectedList = selectedCards.ToList();

        foreach (CardModel card in selectedList)
        {
            await CardPileCmd.Add(card, MainFile.Diary);
        }

        if (selectedList.Count > 0)
        {
            await CardPileCmd.Draw(choiceContext, selectedList.Count, base.Owner);
        }
    }
    
    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner || Pile?.Type != MainFile.Diary)
        {
            return Task.CompletedTask;
        }
        
        canTake = true;
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(2M);
    }
}
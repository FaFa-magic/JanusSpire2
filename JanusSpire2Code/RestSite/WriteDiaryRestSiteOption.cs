using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Relics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.RestSite;

internal sealed class WriteDiaryRestSiteOption(
    Player owner,
    BookAndPen source) : ModRestSiteOptionTemplate(owner)
{
    private const string PlaceholderIconPath =
        "res://JanusSpire2/images/rest_site/WriteDiary.png";

    public override string OptionId => BookAndPen.WriteDiaryOptionId;

    public override RestSiteOptionAssetProfile AssetProfile => new(PlaceholderIconPath);

    public override LocString CustomTitle =>
        new("relics", $"{source.Id.Entry}.writeDiaryOptionName");

    public override LocString Description =>
        new("relics", $"{source.Id.Entry}.writeDiaryOptionDescription");

    public override bool IsEnabled => Owner.Deck.Cards.Any(CanCollect);

    public override async Task<bool> OnSelect()
    {
        CardSelectorPrefs prefs = new(
            new LocString("relics", $"{source.Id.Entry}.writeDiarySelectionScreenPrompt"),
            1)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        CardModel? selectedCard = (await SelectCardWithCollectionPreview(prefs)).FirstOrDefault();
        if (selectedCard == null)
        {
            return false;
        }

        CardCmd.ApplyKeyword(selectedCard, JanusKeywords.Collection);
        source.OnDiaryWritten();
        return true;
    }

    private static bool CanCollect(CardModel card)
    {
        return card.Type != CardType.Quest &&
               card.IsTransformable &&
               !card.Keywords.Contains(JanusKeywords.Collection);
    }

    private static CardTransformation CreateCollectionPreview(CardModel card)
    {
        CardModel preview = (CardModel)card.MutableClone();
        preview.AddKeyword(JanusKeywords.Collection);
        return new CardTransformation(card, preview);
    }

    private async Task<IEnumerable<CardModel>> SelectCardWithCollectionPreview(
        CardSelectorPrefs prefs)
    {
        List<CardModel> cards = Owner.Deck.Cards.Where(CanCollect).ToList();
        if (Owner.Creature.IsDead || cards.Count == 0)
        {
            return [];
        }

        if (!prefs.RequireManualConfirmation && cards.Count <= prefs.MinSelect)
        {
            return cards;
        }

        if (CardSelectCmd.Selector is not null)
        {
            return await CardSelectCmd.Selector.GetSelectedCards(
                cards,
                prefs.MinSelect,
                prefs.MaxSelect);
        }

        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(Owner);
        bool selectsLocally = LocalContext.IsMe(Owner) &&
                              RunManager.Instance.NetService.Type != NetGameType.Replay;
        if (!selectsLocally)
        {
            return (await RunManager.Instance.PlayerChoiceSynchronizer
                .WaitForRemoteChoice(Owner, choiceId)).AsDeckCards();
        }

        IEnumerable<CardModel> selectedCards;
        if (CardSelectCmd.LocalSelector is not null)
        {
            selectedCards = await CardSelectCmd.LocalSelector.GetSelectedCards(
                cards,
                prefs.MinSelect,
                prefs.MaxSelect);
        }
        else
        {
            NDeckTransformSelectScreen screen = NDeckTransformSelectScreen.ShowScreen(
                cards,
                CreateCollectionPreview,
                prefs);
            selectedCards = await screen.CardsSelected();
        }

        List<CardModel> result = selectedCards.ToList();
        RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
            Owner,
            choiceId,
            PlayerChoiceResult.FromMutableDeckCards(result));
        return result;
    }
}

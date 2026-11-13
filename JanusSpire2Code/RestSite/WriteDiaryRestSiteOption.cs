using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Relics;
using JanusSpire2.JanusSpire2Code.Scripts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
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

    private Task<IEnumerable<CardModel>> SelectCardWithCollectionPreview(
        CardSelectorPrefs prefs)
    {
        List<CardModel> cards = Owner.Deck.Cards.Where(CanCollect).ToList();
        return DeckComparisonSelectCmd.Select(Owner, cards, CreateCollectionPreview, prefs);
    }
}

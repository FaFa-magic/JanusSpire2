using Godot;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Relics;

internal sealed class WriteDiaryRestSiteOption(
    Player owner,
    BookAndPen source) : ModRestSiteOptionTemplate(owner)
{
    private const string FutureIconPath =
        "res://JanusSpire2/images/rest_site/WriteDiary.png";

    private const string PlaceholderIconPath =
        "res://JanusSpire2/images/combatui/collection.png";

    public override string OptionId => BookAndPen.WriteDiaryOptionId;

    public override RestSiteOptionAssetProfile AssetProfile => new(
        ResourceLoader.Exists(FutureIconPath) ? FutureIconPath : PlaceholderIconPath);

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

        CardModel? selectedCard = (await CardSelectCmd.FromDeckGeneric(
            Owner,
            prefs,
            CanCollect)).FirstOrDefault();
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
        return !card.Keywords.Contains(JanusKeywords.Collection);
    }
}

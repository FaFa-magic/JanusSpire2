using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models.Capabilities;

namespace JanusSpire2.JanusSpire2Code.Keywords;

[RegisterOwnedCardKeyword(nameof(Perk), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(Collection), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(Counterattack), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(Sticker), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(Transcribe), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None)]
[RegisterOwnedCardKeyword(nameof(Record), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.AfterCardDescription)]
[RegisterOwnedCardKeyword(nameof(Recollection), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None)]
[RegisterOwnedCardKeyword(nameof(Inspiration), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.None)]
public sealed class JanusKeywords
{
    private static bool _persistenceRegistered;

    public static readonly CardKeyword Perk = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Perk)).GetModCardKeyword();
    public static readonly CardKeyword Collection = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Collection)).GetModCardKeyword();
    public static readonly CardKeyword Counterattack = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Counterattack)).GetModCardKeyword();
    public static readonly CardKeyword Sticker = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Sticker)).GetModCardKeyword();
    public static readonly CardKeyword Transcribe = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Transcribe)).GetModCardKeyword();
    public static readonly CardKeyword Record = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Record)).GetModCardKeyword();
    public static readonly CardKeyword Recollection = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Recollection)).GetModCardKeyword();
    public static readonly CardKeyword Inspiration = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Inspiration)).GetModCardKeyword();

    internal static void RegisterPersistence()
    {
        if (_persistenceRegistered)
        {
            return;
        }

        _persistenceRegistered = true;
        ModelSavedDataStore.For(MainFile.ModId)
            .RegisterComputed<CardModel, CollectionKeywordSaveData>(
                "collection_keyword",
                ExportCollectionKeyword,
                ImportCollectionKeyword,
                options: new ModelSavedDataOptions
                {
                    WritePolicy = ModelSavedDataWritePolicy.WhenNonDefault,
                    ClonePolicy = ModelSavedDataClonePolicy.Copy
                });
    }

    private static CollectionKeywordSaveData ExportCollectionKeyword(CardModel card)
    {
        return new CollectionKeywordSaveData
        {
            HasCollection = card.GetKeywordsWithSources(KeywordSources.Local).Contains(Collection)
        };
    }

    private static void ImportCollectionKeyword(CardModel card, CollectionKeywordSaveData? saveData)
    {
        if (saveData?.HasCollection == true &&
            !card.GetKeywordsWithSources(KeywordSources.Local).Contains(Collection))
        {
            card.AddKeyword(Collection);
        }
    }
}

internal sealed class CollectionKeywordSaveData
{
    public bool HasCollection { get; set; }
}

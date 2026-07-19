using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace JanusSpire2.JanusSpire2Code.Keywords;

[RegisterOwnedCardKeyword(nameof(Perk), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(Collection), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(Counterattack), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(Sticker), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
public sealed class JanusKeywords
{
    public static readonly CardKeyword Perk = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Perk)).GetModCardKeyword();
    public static readonly CardKeyword Collection = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Collection)).GetModCardKeyword();
    public static readonly CardKeyword Counterattack = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Counterattack)).GetModCardKeyword();
    public static readonly CardKeyword Sticker = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Sticker)).GetModCardKeyword();
}
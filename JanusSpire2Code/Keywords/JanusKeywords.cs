using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace JanusSpire2.JanusSpire2Code.Keywords;

[RegisterOwnedCardKeyword(nameof(Perk), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
[RegisterOwnedCardKeyword(nameof(Reversible), CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
public sealed class JanusKeywords
{
    public static readonly CardKeyword Perk = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Perk)).GetModCardKeyword();
    
    public static readonly CardKeyword Reversible = ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(Reversible)).GetModCardKeyword();
}
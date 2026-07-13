using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;

namespace JanusSpire2.JanusSpire2Code.Tags;

[RegisterOwnedCardTag(nameof(Scratch))]
[RegisterOwnedCardTag(nameof(DiaryTag))]
public sealed class JanusTags
{
    public static readonly CardTag Scratch = ModContentRegistry.GetQualifiedCardTagId(MainFile.ModId, nameof(Scratch)).GetModCardTag();
    public static readonly CardTag DiaryTag = ModContentRegistry.GetQualifiedCardTagId(MainFile.ModId, nameof(DiaryTag)).GetModCardTag();
}
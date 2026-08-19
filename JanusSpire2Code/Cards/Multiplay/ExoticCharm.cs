using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Multiplay;

public sealed class ExoticCharm() : JanusCardModel(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(3)
    ];
}
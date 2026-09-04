using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using JanusSpire2.JanusSpire2Code.Rewards;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class MagicGloves : JanusRelicModel
{
    internal const int CardCount = 2;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(CardCount)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.Transform)
    ];

    public override Task AfterCombatVictory(CombatRoom room)
    {
        room.AddExtraReward(Owner, new MagicGlovesTransformReward(Owner));
        return Task.CompletedTask;
    }
}

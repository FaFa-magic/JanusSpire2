using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class DrinkCoupon : JanusRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("PotionSlots", 1M)
    ];

    public override async Task AfterRestSiteHeal(Player player, bool isMimicked)
    {
        if (player != Owner)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainMaxPotionCount(DynamicVars["PotionSlots"].IntValue, Owner);
    }

    public override IReadOnlyList<LocString> ModifyExtraRestSiteHealText(
        Player player,
        IReadOnlyList<LocString> currentExtraText)
    {
        if (player != Owner || !LocalContext.IsMe(Owner))
        {
            return currentExtraText;
        }

        LocString? additionalText = AdditionalRestSiteHealText;
        return additionalText is null
            ? currentExtraText
            : [.. currentExtraText, additionalText];
    }
}

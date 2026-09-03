using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class DrinkCoupon : JanusRelicModel
{
    // SerializablePotion and multiplayer potion hovering encode slot indices in four bits.
    // Indices 0-15 are therefore the largest range that is safe across save/network paths.
    private const int MaxNetworkSafePotionSlots = 1 << 4;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("PotionSlots", 1M),
        new DynamicVar("MaxPotionSlots", MaxNetworkSafePotionSlots)
    ];

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not RestSiteRoom)
        {
            return;
        }

        int remainingSafeSlots = MaxNetworkSafePotionSlots - Owner.MaxPotionCount;
        int slotsToGain = DynamicVars["PotionSlots"].IntValue;
        if (slotsToGain > remainingSafeSlots)
        {
            slotsToGain = remainingSafeSlots;
        }

        if (slotsToGain <= 0)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainMaxPotionCount(slotsToGain, Owner);
    }

    public override async Task AfterRestSiteHeal(Player player, bool isMimicked)
    {
        if (player != Owner || !HasOpenNetworkSafePotionSlot(Owner))
        {
            return;
        }

        Flash();
        while (HasOpenNetworkSafePotionSlot(Owner))
        {
            PotionModel potion = PotionFactory.CreateRandomPotionOutOfCombat(
                Owner,
                Owner.RunState.Rng.CombatPotionGeneration).ToMutable();

            if (!(await PotionCmd.TryToProcure(potion, Owner)).success)
            {
                break;
            }
        }
    }

    private static bool HasOpenNetworkSafePotionSlot(Player player)
    {
        int safeSlotCount = player.PotionSlots.Count < MaxNetworkSafePotionSlots
            ? player.PotionSlots.Count
            : MaxNetworkSafePotionSlots;

        for (int i = 0; i < safeSlotCount; i++)
        {
            if (player.PotionSlots[i] == null)
            {
                return true;
            }
        }

        return false;
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

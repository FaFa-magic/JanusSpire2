using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class RemovedRelicRewardAnimationPatch : IPatchMethod
{
    public static string PatchId => "janus_removed_relic_reward_animation";

    public static bool IsCritical => false;

    public static string Description =>
        "Skip the second reward animation for a Relic Fragment that has already been combined";

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NRelicInventory), nameof(NRelicInventory.AnimateRelic))
    ];

    [HarmonyPrefix]
    public static bool Prefix(RelicModel relic)
    {
        return relic is not RelicFragment || relic.Owner.Relics.Contains(relic);
    }
}

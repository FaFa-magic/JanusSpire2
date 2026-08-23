using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class UnceasingTopEmptyHandPatch : IPatchMethod
{
    public static string PatchId => "janus_unceasing_top_disable_empty_hand_draw";

    public static bool IsCritical => false;

    public static string Description => "Disable Unceasing Top's original empty-hand draw effect";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(UnceasingTop),
                nameof(UnceasingTop.AfterHandEmptied))
        ];
    }

    public static bool Prefix(ref Task __result)
    {
        __result = Task.CompletedTask;
        return false;
    }
}

public sealed class UnceasingTopCombatStartPatch : IPatchMethod
{
    public static string PatchId => "janus_unceasing_top_combat_start_confidence";

    public static bool IsCritical => false;

    public static string Description => "Grant one Confidence at combat start for Unceasing Top";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(AbstractModel),
                nameof(AbstractModel.BeforeCombatStart),
                Type.EmptyTypes)
        ];
    }

    public static bool Prefix(AbstractModel __instance, ref Task __result)
    {
        if (__instance is not UnceasingTop unceasingTop)
        {
            return true;
        }

        __result = ApplyConfidence(unceasingTop);
        return false;
    }

    private static async Task ApplyConfidence(UnceasingTop unceasingTop)
    {
        unceasingTop.Flash();
        await PowerCmd.Apply<ConfidencePower>(
            new ThrowingPlayerChoiceContext(),
            unceasingTop.Owner.Creature,
            1M,
            unceasingTop.Owner.Creature,
            null);
    }
}

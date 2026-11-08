using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

// Reset at the shared wrapper boundary, before card selection/animation awaits.
// Lifecycle CardPlaying is later; there is no equivalent early potion event.
public sealed class JanusIdleCardActivityPatch : IPatchMethod
{
    public static string PatchId => "janus_idle_card_activity";
    public static string Description => "Reset the owning Janus idle pose when any card starts playing";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => [new(typeof(CardModel), nameof(CardModel.OnPlayWrapper))];

    [HarmonyPrefix]
    public static void Prefix(CardModel __instance) => NJanusIdleAnimation.NotifyActivity(__instance.Owner);
}

public sealed class JanusIdlePotionActivityPatch : IPatchMethod
{
    public static string PatchId => "janus_idle_potion_activity";
    public static string Description => "Reset the owning Janus idle pose when a potion starts being used";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => [new(typeof(PotionModel), nameof(PotionModel.OnUseWrapper))];

    [HarmonyPrefix]
    public static void Prefix(PotionModel __instance) => NJanusIdleAnimation.NotifyActivity(__instance.Owner);
}

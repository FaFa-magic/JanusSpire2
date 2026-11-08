using Godot;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Nodes.UI;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

// RitsuLib attaches our child at Ready. These narrow hooks only coordinate the
// existing screen/button visual tweens; reward claiming and network logic stay vanilla.
public sealed class JanusBattleRewardsShownPatch : IPatchMethod
{
    public static string PatchId => "janus_battle_rewards_shown";
    public static string Description => "Animate Janus's battle rewards presentation";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => [new(typeof(NRewardsScreen), nameof(NRewardsScreen.AfterOverlayShown))];

    [HarmonyPrefix]
    public static void Prefix(NRewardsScreen __instance) =>
        NJanusBattleRewardsController.Find(__instance)?.ShowPresentation();

    [HarmonyPostfix]
    public static void Postfix(NRewardsScreen __instance, ref Tween? ____fadeTween) =>
        NJanusBattleRewardsController.Find(__instance)?.AnimatePresentation(ref ____fadeTween);
}

public sealed class JanusBattleRewardsHiddenPatch : IPatchMethod
{
    public static string PatchId => "janus_battle_rewards_hidden";
    public static string Description => "Hide Janus decoration under other overlays";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => [new(typeof(NRewardsScreen), nameof(NRewardsScreen.AfterOverlayHidden))];

    [HarmonyPostfix]
    public static void Postfix(NRewardsScreen __instance) =>
        NJanusBattleRewardsController.Find(__instance)?.HidePresentation();
}

public sealed class JanusBattleRewardsProceedShowPatch : IPatchMethod
{
    public static string PatchId => "janus_battle_rewards_proceed_show";
    public static string Description => "Keep the original proceed button in Janus's reward layout";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => [new(typeof(NProceedButton), "get_ShowPos")];

    [HarmonyPrefix]
    public static bool Prefix(NProceedButton __instance, ref Vector2 __result) =>
        JanusBattleRewardsButtonPlacement.TryPosition(__instance, false, ref __result);
}

public sealed class JanusBattleRewardsProceedHidePatch : IPatchMethod
{
    public static string PatchId => "janus_battle_rewards_proceed_hide";
    public static string Description => "Keep original proceed disable/skip/readiness behavior";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => [new(typeof(NProceedButton), "get_HidePos")];

    [HarmonyPrefix]
    public static bool Prefix(NProceedButton __instance, ref Vector2 __result) =>
        JanusBattleRewardsButtonPlacement.TryPosition(__instance, true, ref __result);
}

internal static class JanusBattleRewardsButtonPlacement
{
    public static bool TryPosition(NProceedButton button, bool hidden, ref Vector2 result)
    {
        if (button.GetParent() is not NRewardsScreen screen ||
            NJanusBattleRewardsController.Find(screen) is not { } presentation)
            return true;
        result = presentation.ProceedPosition(hidden);
        return false;
    }
}

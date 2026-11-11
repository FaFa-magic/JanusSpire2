using Godot;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Nodes.UI;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Flavor;
using MegaCrit.Sts2.Core.Nodes.Reaction;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class JanusReactionWheelPatch : IPatchMethod
{
    public static string PatchId => "janus_reaction_wheel";
    public static string Description => "Center enlarged Janus reactions on a compact combat wheel";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NReactionWheel), nameof(NReactionWheel._Input), [typeof(InputEvent)])];

    private static readonly StringName ReactWheelAction = new("react_wheel");

    [HarmonyPostfix]
    public static void Postfix(NReactionWheel __instance, InputEvent __0)
    {
        if (__instance.IsVisibleInTree() && __0.IsActionPressed(ReactWheelAction))
            JanusReactions.StyleWheel(__instance, JanusReactions.ShouldShowJanusArt(LocalContext.NetId));
    }
}

public sealed class JanusLocalReactionPatch : IPatchMethod
{
    public static string PatchId => "janus_local_reaction_art";
    public static string Description => "Style the native popup after sending a local Janus reaction";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NReactionContainer), nameof(NReactionContainer.DoLocalReaction), [typeof(Texture2D), typeof(Vector2)])];

    [HarmonyPrefix]
    public static void Prefix(NReactionContainer __instance, out int __state)
        => __state = JanusReactions.NextPopupIndex(__instance, LocalContext.NetId);

    [HarmonyPostfix]
    public static void Postfix(NReactionContainer __instance, int __state)
        => JanusReactions.StyleAddedPopup(__instance, __state);
}

public sealed class JanusRemoteReactionPatch : IPatchMethod
{
    public static string PatchId => "janus_remote_reaction_art";
    public static string Description => "Style the native popup for a received Janus reaction";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(ReactionSynchronizer), "HandleReactionMessage", [typeof(ReactionMessage), typeof(ulong)])];

    [HarmonyPrefix]
    public static void Prefix(NReactionContainer ____container, ulong __1, out int __state)
        => __state = JanusReactions.NextPopupIndex(____container, __1);

    [HarmonyPostfix]
    public static void Postfix(NReactionContainer ____container, int __state)
        => JanusReactions.StyleAddedPopup(____container, __state);
}

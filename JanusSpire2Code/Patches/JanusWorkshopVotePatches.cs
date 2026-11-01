using System.Reflection;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Characters;
using JanusSpire2.JanusSpire2Code.Workshop;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class JanusWorkshopVoteSelectionPatch : IPatchMethod
{
    public static string PatchId => "janus_workshop_vote_selection";
    public static string Description => "Update the local Janus workshop vote button on character selection";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter),
            [typeof(NCharacterSelectButton), typeof(CharacterModel)])
    ];

    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance, NCharacterSelectButton charSelectButton, CharacterModel characterModel) =>
        JanusWorkshopVoteUi.GetButton(__instance)?.SetCharacterSelected(
            characterModel is JanusCharacter && !charSelectButton.IsLocked && !charSelectButton.IsRandom);
}

public sealed class JanusWorkshopVoteScreenPatch : IPatchMethod
{
    public static string PatchId => "janus_workshop_vote_screen_lifecycle";
    public static string Description => "Reset local workshop vote requests when opening or leaving character select";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.OnSubmenuOpened), []),
        new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.OnSubmenuClosed), [])
    ];

    [HarmonyPrefix]
    public static void Prefix(NCharacterSelectScreen __instance, MethodBase __originalMethod)
    {
        var button = JanusWorkshopVoteUi.GetButton(__instance);
        if (__originalMethod.Name == nameof(NCharacterSelectScreen.OnSubmenuOpened))
            button?.OpenScreen();
        else
            button?.CloseScreen();
    }
}

public sealed class JanusWorkshopVoteInteractablePatch : IPatchMethod
{
    public static string PatchId => "janus_workshop_vote_interactable";
    public static string Description => "Hide the local workshop vote button while ready or embarking";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.MethodName.OnEmbarkPressed), [typeof(NButton)]),
        new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.MethodName.OnUnreadyPressed), [typeof(NButton)])
    ];

    [HarmonyPostfix]
    public static void Postfix(NCharacterSelectScreen __instance, MethodBase __originalMethod) =>
        JanusWorkshopVoteUi.GetButton(__instance)?.SetInteractable(
            __originalMethod.Name == nameof(NCharacterSelectScreen.MethodName.OnUnreadyPressed));
}

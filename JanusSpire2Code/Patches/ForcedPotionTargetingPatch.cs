using Godot;
using JanusSpire2.JanusSpire2Code.Relics;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class ForcedPotionTargetingPatch : IPatchMethod
{
    public static string PatchId => "janus_forced_potion_targeting";

    public static bool IsCritical => false;

    public static string Description => "Prevent cancel input during Afternoon Tea Supply potion targeting";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(NTargetManager),
                nameof(NTargetManager._Input),
                [typeof(InputEvent)])
        ];
    }

    public static bool Prefix(NTargetManager __instance, InputEvent inputEvent)
    {
        if (!AfternoonTeaSupply.IsForcingPotionTargetSelection || !IsCancelInput(inputEvent))
        {
            return true;
        }

        __instance.GetViewport()?.SetInputAsHandled();
        return false;
    }

    private static bool IsCancelInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton
            {
                ButtonIndex: MouseButton.Right
            } mouseButton)
        {
            return mouseButton.IsPressed();
        }

        return inputEvent.IsActionPressed(MegaInput.cancel) ||
               inputEvent.IsActionPressed(MegaInput.pauseAndBack) ||
               inputEvent.IsActionPressed(MegaInput.topPanel);
    }
}

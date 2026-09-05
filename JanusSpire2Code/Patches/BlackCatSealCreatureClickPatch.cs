using Godot;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

/// <summary>
/// Collects local left-click input from a combat creature's model. Gameplay is not
/// mutated here: accepted clicks become synchronized, play-phase-only actions in
/// <see cref="BlackCatSealPower"/>.
/// </summary>
public sealed class BlackCatSealCreatureClickPatch : IPatchMethod
{
    public static string PatchId => "janus_black_cat_seal_creature_left_click";

    public static string Description =>
        "Bloom Black Cat Seal by left-clicking its player or enemy creature model";

    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCreature), nameof(NCreature._Ready))
    ];

    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {
        __instance.Hitbox.Connect(
            Control.SignalName.GuiInput,
            Callable.From<InputEvent>(inputEvent => OnGuiInput(__instance, inputEvent)));
    }

    private static void OnGuiInput(NCreature creatureNode, InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton
            {
                ButtonIndex: MouseButton.Left
            } mouseButton ||
            !mouseButton.IsReleased())
        {
            return;
        }

        Creature target = creatureNode.Entity;
        ICombatState? combatState = target.CombatState;
        if (combatState == null ||
            target.GetPower<BlackCatSealPower>() == null ||
            !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        NTargetManager targetManager = NTargetManager.Instance;
        if (targetManager.IsInSelection ||
            targetManager.LastTargetingFinishedFrame == creatureNode.GetTree().GetFrame())
        {
            // Do not turn the release that selected a card/potion target into a bloom.
            return;
        }

        Player? requester = LocalContext.GetMe(combatState);
        if (requester == null ||
            !BlackCatSealPower.TryRequestManualBloom(requester, target))
        {
            return;
        }

        creatureNode.GetViewport().SetInputAsHandled();
    }
}

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
/// Collects consecutive local left-click input from a combat creature's model. Gameplay
/// is not mutated here: accepted click sequences become synchronized, play-phase-only
/// actions in <see cref="BlackCatSealPower"/>.
/// </summary>
public sealed class BlackCatSealCreatureClickPatch : IPatchMethod
{
    private const ulong MultiClickWindowMsec = 600;

    private static ClickSequence? _clickSequence;
    private static uint? _suppressedTargetCombatId;

    public static string PatchId => "janus_black_cat_seal_creature_double_click";

    public static string Description =>
        "Bloom Black Cat Seal after consecutive clicks on its player or enemy model";

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
                ButtonIndex: MouseButton.Left,
                Pressed: true
            } mouseButton)
        {
            return;
        }

        Creature target = creatureNode.Entity;
        ICombatState? combatState = target.CombatState;
        if (combatState == null ||
            target.CombatId is not { } targetCombatId ||
            !CombatManager.Instance.IsInProgress)
        {
            ResetClickTracking();
            return;
        }

        NTargetManager targetManager = NTargetManager.Instance;
        if (targetManager.IsInSelection ||
            targetManager.LastTargetingFinishedFrame == creatureNode.GetTree().GetFrame())
        {
            // A press consumed by card or potion targeting never counts toward blooming.
            _clickSequence = null;
            _suppressedTargetCombatId = targetCombatId;
            return;
        }

        ulong nowMsec = Time.GetTicksMsec();
        if (_suppressedTargetCombatId.HasValue)
        {
            // Start a fresh sequence after targeting. This allows the next press in the
            // same window to bloom without reusing the press that selected the target.
            _suppressedTargetCombatId = null;
            _clickSequence = new(targetCombatId, nowMsec, RequestIssued: false);
            return;
        }

        ClickSequence? previous = _clickSequence;
        ClickSequence previousValue = previous.GetValueOrDefault();
        bool previousIsRecent = previous is { } recent &&
                                nowMsec >= recent.LastPressMsec &&
                                nowMsec - recent.LastPressMsec <= MultiClickWindowMsec;
        bool sameTargetSequence = previousIsRecent &&
                                  previousValue.TargetCombatId == targetCombatId;
        bool nativeDoubleWithoutObservedFirstPress = mouseButton.DoubleClick &&
                                                     !previousIsRecent;

        bool requestIssued = sameTargetSequence && previousValue.RequestIssued;
        _clickSequence = new(targetCombatId, nowMsec, requestIssued);

        if ((!sameTargetSequence && !nativeDoubleWithoutObservedFirstPress) || requestIssued)
        {
            return;
        }

        Player? requester = LocalContext.GetMe(combatState);
        if (requester == null ||
            target.GetPower<BlackCatSealPower>() == null)
        {
            return;
        }

        if (!BlackCatSealPower.TryRequestManualBloom(requester, target))
        {
            return;
        }

        _clickSequence = new(targetCombatId, nowMsec, RequestIssued: true);
        creatureNode.GetViewport().SetInputAsHandled();
    }

    private static void ResetClickTracking()
    {
        _clickSequence = null;
        _suppressedTargetCombatId = null;
    }

    private readonly record struct ClickSequence(
        uint TargetCombatId,
        ulong LastPressMsec,
        bool RequestIssued);
}

using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

/// <summary>
/// RitsuLib 0.6.10 can arrange cards synchronously from Unfocused while Reparent
/// still locks the old card layer. Queue only Janus's projection-hand layout;
/// the original layout runs against the current holders after the tree unlocks.
/// Keep RitsuLib's active-holder exclusion, right-over-left order and targeting
/// rollback intact; this patch changes only when its original layout executes.
/// </summary>
public sealed class RecordExtraHandLayoutPatch : IPatchMethod
{
    public static string PatchId => "janus_record_extra_hand_deferred_layout";
    public static string Description => "Defer Janus record-hand layout until card reparenting has finished";
    public static bool IsCritical => false;
    public static ModPatchTarget[] GetTargets() => [new(typeof(NModExtraHand), "ArrangeCards", Type.EmptyTypes)];

    private static readonly MethodInfo ArrangeMethod = AccessTools.DeclaredMethod(typeof(NModExtraHand), "ArrangeCards", Type.EmptyTypes);
    private static readonly ConditionalWeakTable<NModExtraHand, PendingLayout> Pending = new();

    [HarmonyPrefix]
    public static bool Prefix(NModExtraHand __instance)
    {
        if (__instance.Definition?.PileType != MainFile.RecordExtraHand)
            return true;

        var request = Pending.GetOrCreateValue(__instance);
        // Consume this permit before entering the original method. A nested
        // focus callback queues another pass instead of re-entering the layout.
        if (request.RunOriginalOnce)
        {
            request.RunOriginalOnce = false;
            return true;
        }
        if (request.Queued || !CanArrange(__instance))
            return false;

        request.Queued = true;
        Callable.From(() =>
        {
            request.Queued = false;
            if (!CanArrange(__instance))
                return;
            request.RunOriginalOnce = true;
            try
            {
                ArrangeMethod.Invoke(__instance, null);
            }
            finally
            {
                request.RunOriginalOnce = false;
            }
        }).CallDeferred();
        return false;
    }

    private static bool CanArrange(NModExtraHand hand)
        => GodotObject.IsInstanceValid(hand) && hand.IsInsideTree() && !hand.IsQueuedForDeletion();

    private sealed class PendingLayout
    {
        public bool Queued;
        public bool RunOriginalOnce;
    }
}

using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Relics;
using JanusSpire2.JanusSpire2Code.Singleton;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

/// <summary>
/// Opens an async-local scope only while Centennial Puzzle is performing the draw
/// that must survive Janus's delayed hand flush.
/// </summary>
public sealed class CentennialPuzzleDelayedFlushPatch : IPatchMethod
{
    public static string PatchId => "janus_centennial_puzzle_delayed_flush_retain";

    public static string Description =>
        "Scope Centennial Puzzle draws that must survive one Janus delayed hand flush";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(
            typeof(CentennialPuzzle),
            nameof(CentennialPuzzle.AfterDamageReceived),
            [
                typeof(PlayerChoiceContext),
                typeof(Creature),
                typeof(DamageResult),
                typeof(ValueProp),
                typeof(Creature),
                typeof(CardModel)
            ])
    ];

    [HarmonyPriority(Priority.Last)]
    [HarmonyPrefix]
    public static void Prefix(
        CentennialPuzzle __instance,
        Creature target,
        DamageResult result,
        out IDisposable? __state)
    {
        __state = null;
        if (!CombatManager.Instance.IsInProgress ||
            target != __instance.Owner.Creature ||
            result.UnblockedDamage <= 0 ||
            __instance.UsedThisCombat ||
            !__instance.Owner.Relics.Any(relic => relic is ISkipPlayerFlushRelic))
        {
            return;
        }

        __state = CentennialPuzzleDrawScope.Enter();
    }

    // A finalizer restores the caller's execution context even if another patch throws.
    // The async state machine has already captured the scope before this method runs.
    public static void Finalizer(IDisposable? __state)
    {
        __state?.Dispose();
    }
}

/// <summary>
/// Captures the exact card returned by Centennial Puzzle's single-card Draw call.
/// Nested draws caused by draw hooks are excluded by the scoped call depth.
/// </summary>
public sealed class CentennialPuzzleDrawCapturePatch : IPatchMethod
{
    public static string PatchId => "janus_centennial_puzzle_delayed_flush_capture";

    public static string Description =>
        "Protect the card directly drawn by Centennial Puzzle from one delayed hand flush";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(
            typeof(CardPileCmd),
            nameof(CardPileCmd.Draw),
            [typeof(PlayerChoiceContext), typeof(Player)])
    ];

    [HarmonyPrefix]
    internal static void Prefix(out CentennialPuzzleDrawScope.DrawCall? __state)
    {
        __state = CentennialPuzzleDrawScope.TryBeginDraw();
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    internal static void Postfix(
        ref Task<CardModel?> __result,
        CentennialPuzzleDrawScope.DrawCall? __state)
    {
        if (__state is not null)
        {
            __result = CaptureDrawnCard(__result, __state);
        }
    }

    internal static void Finalizer(CentennialPuzzleDrawScope.DrawCall? __state)
    {
        __state?.RestoreCallerDepth();
    }

    private static async Task<CardModel?> CaptureDrawnCard(
        Task<CardModel?> originalTask,
        CentennialPuzzleDrawScope.DrawCall drawCall)
    {
        CardModel? card = await originalTask;
        if (drawCall.Depth == 1 && card?.Pile?.Type == PileType.Hand)
        {
            JanusSingleton.ProtectFromNextDelayedFlush(card);
        }

        return card;
    }
}

internal static class CentennialPuzzleDrawScope
{
    private static readonly AsyncLocal<Scope?> Current = new();
    private static readonly AsyncLocal<int> CurrentDrawDepth = new();

    internal static IDisposable Enter()
    {
        Scope? previous = Current.Value;
        Scope scope = new();
        Current.Value = scope;
        return new ScopeLease(scope, previous);
    }

    internal static DrawCall? TryBeginDraw()
    {
        Scope? scope = Current.Value;
        if (scope is null)
        {
            return null;
        }

        int previousDepth = CurrentDrawDepth.Value;
        int depth = previousDepth + 1;
        CurrentDrawDepth.Value = depth;
        return new DrawCall(depth, previousDepth);
    }

    internal sealed class Scope
    {
    }

    private sealed class ScopeLease(Scope scope, Scope? previous) : IDisposable
    {
        public void Dispose()
        {
            if (ReferenceEquals(Current.Value, scope))
            {
                Current.Value = previous;
            }
        }
    }

    internal sealed class DrawCall(int depth, int previousDepth)
    {
        internal int Depth { get; } = depth;

        internal void RestoreCallerDepth()
        {
            if (CurrentDrawDepth.Value == Depth)
            {
                CurrentDrawDepth.Value = previousDepth;
            }
        }
    }
}

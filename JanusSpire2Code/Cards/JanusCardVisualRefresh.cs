using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.TestSupport;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace JanusSpire2.JanusSpire2Code.Cards;

// UI-only requests: never create cards, change piles, or alter synchronized model state.
internal static class JanusCardVisualRefresh
{
    private static readonly HashSet<CardModel> Pending = new(ReferenceEqualityComparer.Instance);

    internal static void RequestTextRefresh(this CardModel model)
    {
        if (TestMode.IsOn)
            return;

        bool schedule;
        lock (Pending)
        {
            schedule = Pending.Count == 0;
            Pending.Add(model);
        }

        if (schedule)
            Callable.From(ScheduleNextFrame).CallDeferred();
    }

    internal static void RequestInstanceAssetReload(this CardModel model)
    {
        if (TestMode.IsOn)
            return;

        // The framework's RequestVisualReload also matches by model ID. All projections
        // share an ID, so use reference identity (including only this card's projection).
        RuntimeAssetRefreshCoordinator.RequestCardsWhere(candidate =>
            ReferenceEquals(candidate, model) ||
            candidate is JanusRecordMappingCard mapping && ReferenceEquals(mapping.Original, model));
        model.RequestTextRefresh();
    }

    private static void ScheduleNextFrame()
    {
        if (Engine.GetMainLoop() is SceneTree tree)
            tree.Connect(SceneTree.SignalName.ProcessFrame, Callable.From(FlushTextRefreshes),
                (uint)GodotObject.ConnectFlags.OneShot);
        else
            FlushTextRefreshes();
    }

    private static void FlushTextRefreshes()
    {
        HashSet<CardModel> changed;
        lock (Pending)
        {
            changed = new(Pending, ReferenceEqualityComparer.Instance);
            Pending.Clear();
        }

        if (Engine.GetMainLoop() is not SceneTree tree || !GodotObject.IsInstanceValid(tree.Root))
            return;

        // One traversal per batch, rather than one lookup/reload per changed card.
        var nodes = new Stack<Node>();
        nodes.Push(tree.Root);
        while (nodes.TryPop(out Node? node))
        {
            if (!GodotObject.IsInstanceValid(node) || node.IsQueuedForDeletion())
                continue;

            if (node is NCard { Model: { } model } card && card.IsInsideTree() && card.IsNodeReady() &&
                (changed.Contains(model) ||
                 model is JanusRecordMappingCard { Original: { } original } && changed.Contains(original)))
            {
                // Keep the official target/visibility/preview pipeline, but do not call
                // Reload: counters must not reset portrait materials or rebuild overlays.
                card.UpdateVisuals(card.DisplayingPile, model.UpgradePreviewType == CardUpgradePreviewType.None
                    ? CardPreviewMode.Normal
                    : CardPreviewMode.Upgrade);
            }

            for (int i = node.GetChildCount() - 1; i >= 0; i--)
                nodes.Push(node.GetChild(i));
        }
    }
}

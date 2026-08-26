using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Cards;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

internal static class RecordMappingPlayRedirect
{
    private static readonly HashSet<JanusRecordCardModel> QueuedOriginals = [];

    internal static void MarkQueued(JanusRecordCardModel original)
    {
        QueuedOriginals.Add(original);
    }

    internal static bool ConsumeQueued(JanusRecordCardModel original)
    {
        return QueuedOriginals.Remove(original);
    }

    internal static bool IsQueued(JanusRecordCardModel original)
    {
        return QueuedOriginals.Contains(original);
    }

}

/// <summary>
/// Presentation mappings are required as backing models for RitsuLib's extra-hand UI, but they
/// are not gameplay cards. Exclude them from the player's aggregate card enumeration so card
/// counts, hooks, searches, and mass effects only see the Diary originals.
/// </summary>
public sealed class RecordMappingAllCardsPatch : IPatchMethod
{
    public static string PatchId => "janus_record_mapping_exclude_from_all_cards";

    public static string Description => "Exclude Record presentation mappings from gameplay card enumeration";

    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(PlayerCombatState), nameof(PlayerCombatState.AllCards), MethodType.Getter)];
    }

    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<CardModel> __result)
    {
        __result = __result.Where(card => card is not JanusRecordMappingCard);
    }
}

/// <summary>
/// The extra-hand card is deliberately only a local presentation handle. Sending its runtime
/// CombatCardIndex over the network is unsafe because independently-created handles do not have
/// matching indices on every peer. Rebind its holder to the already-synchronised Diary original
/// before vanilla constructs the PlayCardAction.
/// </summary>
public sealed class RecordMappingCardPlayPatch : IPatchMethod
{
    public static string PatchId => "janus_record_mapping_card_play";

    public static string Description => "Replace a Record presentation holder with its Diary original before play";

    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NCardPlay), "TryPlayCard", [typeof(Creature)])];
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static bool Prefix(NCardPlay __instance, Creature? target)
    {
        if (__instance.Holder.CardModel is not JanusRecordMappingCard mapping)
        {
            return true;
        }

        JanusRecordCardModel? original = mapping.Original;
        if ((mapping.Pile != null && mapping.Pile.Type != PileType.Hand) ||
            original == null ||
            !ReferenceEquals(original.Owner, mapping.Owner) ||
            original.Pile?.Type != MainFile.Diary ||
            !original.CanTake ||
            !original.CanPlayTargeting(target))
        {
            // A presentation card must never fall through to vanilla play by itself.
            __instance.CancelPlayCard();
            return false;
        }

        ICombatState combatState = mapping.CombatState!;
        CardPile extraHand = MainFile.RecordExtraHand.GetPile(mapping.Owner);
        var mappingHolder = __instance.Holder;

        // The mapping is only a local targeting handle. Keep the real card in Diary while its
        // PlayCardAction is waiting in the synchronized queue; moving it to Hand here would make
        // only the requesting peer's checksum change while another player's action is executing.
        mapping.Pile?.RemoveInternal(mapping, true);
        if (mappingHolder.CardNode != null)
        {
            mappingHolder.CardNode.Model = original;
        }

        RecordMappingPlayRedirect.MarkQueued(original);
        bool handled = false;
        void OnTargetingFinished(bool success)
        {
            if (handled)
            {
                return;
            }

            handled = true;
            if (success)
            {
                if (combatState.ContainsCard(mapping))
                {
                    combatState.RemoveCard(mapping);
                }

                return;
            }

            RecordMappingPlayRedirect.ConsumeQueued(original);
            MoveOriginalToDiary(original);

            if (mapping.Pile == null)
            {
                extraHand.AddInternal(mapping, -1, true);
            }

            if (mappingHolder.CardNode != null)
            {
                mappingHolder.CardNode.Model = mapping;
            }

            RecordExtraHandManager.SyncFor(original);
        }

        __instance.Connect(NCardPlay.SignalName.Finished, Callable.From<bool>(OnTargetingFinished));
        return true;
    }

    internal static void RestoreOriginalToDiary(JanusRecordCardModel original)
    {
        MoveOriginalToDiary(original);
        RecordExtraHandManager.SyncFor(original);
    }

    private static void MoveOriginalToDiary(JanusRecordCardModel original)
    {
        if (original.CanTake && original.Pile?.Type == PileType.Hand)
        {
            original.Pile.RemoveInternal(original, true);
            MainFile.Diary.GetPile(original.Owner).AddInternal(original);
        }
    }
}

/// <summary>
/// RitsuLib normally keeps an extra-hand card in the vanilla Hand pile for the whole targeting
/// interaction. A Record mapping is presentation-only, so remove it again as soon as vanilla has
/// created the targeting node. This prevents a teammate action checksum from observing a local-only
/// mapping in Hand while the player is still dragging or choosing a target.
/// </summary>
public sealed class RecordMappingTargetingStatePatch : IPatchMethod
{
    private static readonly AccessTools.FieldRef<NPlayerHand, NCardPlay?> CurrentCardPlayRef =
        AccessTools.FieldRefAccess<NPlayerHand, NCardPlay?>("_currentCardPlay");

    public static string PatchId => "janus_record_mapping_targeting_state";

    public static string Description => "Keep Record presentation mappings out of Hand during targeting";

    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(NPlayerHand),
                "StartCardPlay",
                [typeof(NHandCardHolder), typeof(bool)])
        ];
    }

    [HarmonyPostfix]
    public static void Postfix(NPlayerHand __instance, NHandCardHolder holder)
    {
        if (holder.CardModel is not JanusRecordMappingCard mapping ||
            mapping.Pile?.Type != PileType.Hand ||
            mapping.Original is not { } original ||
            mapping.CombatState is not { } combatState)
        {
            return;
        }

        NCardPlay? cardPlay = CurrentCardPlayRef(__instance);
        if (cardPlay == null || !GodotObject.IsInstanceValid(cardPlay) ||
            !ReferenceEquals(cardPlay.Holder, holder))
        {
            return;
        }

        mapping.Pile.RemoveInternal(mapping, true);
        bool handled = false;
        void OnTargetingFinished(bool success)
        {
            if (handled)
            {
                return;
            }

            handled = true;
            if (success || mapping.Pile != null || !combatState.ContainsCard(mapping))
            {
                return;
            }

            if (holder.CardNode != null)
            {
                holder.CardNode.Model = mapping;
            }

            MainFile.RecordExtraHand.GetPile(mapping.Owner).AddInternal(mapping, -1, true);
            RecordExtraHandManager.SyncFor(original);

            // NPlayerHand's own Finished callback runs first and returns this holder to the
            // vanilla hand UI. The mod pile has already created its replacement holder, so remove
            // the stale vanilla holder after signal dispatch completes.
            Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(holder) &&
                    ReferenceEquals(__instance.GetCardHolder(mapping), holder))
                {
                    __instance.RemoveCardHolder(holder);
                }
            }).CallDeferred();
        }

        cardPlay.Connect(NCardPlay.SignalName.Finished, Callable.From<bool>(OnTargetingFinished));
    }
}

/// <summary>
/// Handles cancellation after vanilla has queued the Diary original but before it starts executing.
/// The synchronized enqueue event exists on every peer, unlike the local targeting request. Use it
/// to mark the Diary original as pending and rebuild its projection if the official queue cancels it.
/// </summary>
public sealed class RecordMappingActionEnqueuePatch : IPatchMethod
{
    public static string PatchId => "janus_record_mapping_action_enqueue";

    public static string Description => "Track synchronized Record mapping actions and restore canceled plays";

    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(ActionQueueSet),
                nameof(ActionQueueSet.EnqueueWithoutSynchronizing),
                [typeof(GameAction)])
        ];
    }

    [HarmonyPrefix]
    public static void Prefix(GameAction gameAction)
    {
        if (gameAction is not PlayCardAction action ||
            action.NetCombatCard.ToCardModelOrNull() is not JanusRecordCardModel original ||
            (!RecordMappingPlayRedirect.IsQueued(original) &&
             (!original.CanTake || original.Pile?.Type != MainFile.Diary)))
        {
            return;
        }

        RecordMappingPlayRedirect.MarkQueued(original);
        bool handled = false;
        action.BeforeCancelled += _ =>
        {
            if (!handled && RecordMappingPlayRedirect.ConsumeQueued(original))
            {
                handled = true;
                RecordMappingCardPlayPatch.RestoreOriginalToDiary(original);
            }
        };
    }
}

/// <summary>
/// Vanilla queue presentation expects a locally played card to be in Hand while it captures and
/// removes the targeting holder. A mapped Record card must remain in Diary for deterministic game
/// state, so expose it as Hand only for this synchronous UI method and restore it immediately.
/// </summary>
public sealed class RecordMappingLocalQueueVisualPatch : IPatchMethod
{
    public static string PatchId => "janus_record_mapping_local_queue_visual";

    public static string Description => "Queue a Record mapping holder without leaving its original in Hand";

    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(NCardPlayQueue),
                nameof(NCardPlayQueue.OnLocalCardPlayed),
                [typeof(PlayCardAction), typeof(NCardHolder), typeof(CardModel)])
        ];
    }

    [HarmonyPrefix]
    public static void Prefix(CardModel card, ref bool __state)
    {
        __state = card is JanusRecordCardModel original &&
                  RecordMappingPlayRedirect.IsQueued(original) &&
                  original.Pile?.Type == MainFile.Diary;
        if (!__state)
        {
            return;
        }

        card.Pile!.RemoveInternal(card, true);
        PileType.Hand.GetPile(card.Owner).AddInternal(card, -1, true);
    }

    [HarmonyPostfix]
    public static void Postfix(CardModel card, bool __state)
    {
        if (!__state || card.Pile?.Type != PileType.Hand)
        {
            return;
        }

        card.Pile.RemoveInternal(card, true);
        MainFile.Diary.GetPile(card.Owner).AddInternal(card);
    }
}

/// <summary>
/// Every peer receives the stable Diary original in the PlayCardAction. All peers keep it in Diary
/// while the action is queued, then perform the same transfer here before card logic or
/// checksum-relevant hooks run.
/// </summary>
public sealed class RecordMappingPlayActionPatch : IPatchMethod
{
    public static string PatchId => "janus_record_mapping_play_action";

    public static string Description => "Move a networked Record original out of Diary before mapped play";

    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(PlayCardAction), "ExecuteAction", Type.EmptyTypes)];
    }

    [HarmonyPrefix]
    public static bool Prefix(PlayCardAction __instance, ref Task __result)
    {
        CardModel? actionCard = __instance.NetCombatCard.ToCardModelOrNull();
        if (actionCard is JanusRecordMappingCard)
        {
            // Last-resort invariant: a presentation card may be targeted and dragged, but may
            // never execute, enter play history, or resolve into Discard/Exhaust.
            __instance.Cancel();
            __result = Task.CompletedTask;
            return false;
        }

        if (actionCard is not JanusRecordCardModel original)
        {
            return true;
        }

        bool wasQueued = RecordMappingPlayRedirect.ConsumeQueued(original);
        bool isRemoteDiaryPlay = original.CanTake && original.Pile?.Type == MainFile.Diary;
        if (!wasQueued && !isRemoteDiaryPlay)
        {
            return true;
        }

        if (!original.CanTake || !ReferenceEquals(original.Owner, __instance.Player))
        {
            RecordMappingCardPlayPatch.RestoreOriginalToDiary(original);
            __instance.Cancel();
            __result = Task.CompletedTask;
            return false;
        }

        if (original.Pile?.Type == MainFile.Diary)
        {
            original.Pile.RemoveInternal(original);
            PileType.Hand.GetPile(original.Owner).AddInternal(original, -1, true);
        }

        if (original.Pile?.Type != PileType.Hand)
        {
            __instance.Cancel();
            __result = Task.CompletedTask;
            return false;
        }

        // Remove each peer's own presentation handle before card hooks run. The handle's local
        // runtime index is never used as network state.
        RecordExtraHandManager.RemoveMappingFor(original);

        bool cleanedUp = false;
        void Cleanup(GameAction _)
        {
            if (cleanedUp)
            {
                return;
            }

            cleanedUp = true;
            RecordMappingCardPlayPatch.RestoreOriginalToDiary(original);
        }

        __instance.AfterFinished += Cleanup;
        __instance.BeforeCancelled += Cleanup;
        return true;
    }
}

using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

/// <summary>
/// Presentation mappings must not affect card-counting mechanics.
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
/// Presentation mappings are stored in combat piles so RitsuLib and NetCombatCardDb can own them,
/// but they must never become combat-hook listeners themselves.
/// </summary>
public sealed class RecordMappingHookListenersPatch : IPatchMethod
{
    public static string PatchId => "janus_record_mapping_exclude_from_hook_listeners";
    public static string Description => "Exclude Record presentation mappings from combat hooks";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CombatState), nameof(CombatState.IterateHookListeners), Type.EmptyTypes)];
    }

    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<AbstractModel> __result)
    {
        __result = __result.Where(model => model is not JanusRecordMappingCard);
    }
}

/// <summary>
/// Moving a presentation mapping through RitsuLib's official CardPileCmd entry path is required
/// for its holder lifecycle, but that movement is not a gameplay card-pile event.
/// </summary>
public sealed class RecordMappingPileHookPatch : IPatchMethod
{
    public static string PatchId => "janus_record_mapping_suppress_pile_hooks";
    public static string Description => "Suppress gameplay pile hooks for Record presentation mappings";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(Hook),
                nameof(Hook.AfterCardChangedPiles),
                [typeof(IRunState), typeof(ICombatState), typeof(CardModel), typeof(PileType), typeof(AbstractModel)])
        ];
    }

    [HarmonyPrefix]
    public static bool Prefix(CardModel card, ref Task __result)
    {
        if (card is not JanusRecordMappingCard)
        {
            return true;
        }

        __result = Task.CompletedTask;
        return false;
    }
}

/// <summary>
/// RitsuLib 0.5.17 keeps an extra-hand card in its source pile while targeting and queued.
/// Let it play the mapping model normally, but make all playability checks use the Diary original.
/// </summary>
public sealed class RecordMappingCanPlayPatch : IPatchMethod
{
    public static string PatchId => "janus_record_mapping_can_play";
    public static string Description => "Delegate Record mapping playability to its Diary original";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(CardModel),
                nameof(CardModel.CanPlay),
                [typeof(UnplayableReason).MakeByRefType(), typeof(AbstractModel).MakeByRefType()])
        ];
    }

    [HarmonyPrefix]
    public static bool Prefix(
        CardModel __instance,
        ref UnplayableReason reason,
        ref AbstractModel? preventer,
        ref bool __result)
    {
        if (__instance is not JanusRecordMappingCard mapping)
        {
            return true;
        }

        JanusRecordCardModel? original = RecordExtraHandManager.ResolveOriginal(mapping);
        if (original is { CanTake: true } &&
            original.Pile?.Type == MainFile.Diary &&
            ReferenceEquals(original.Owner, mapping.Owner))
        {
            __result = original.CanPlay(out reason, out preventer);
            return false;
        }

        reason = UnplayableReason.BlockedByCardLogic;
        preventer = mapping;
        __result = false;
        return false;
    }
}

/// <summary>
/// The synchronized action contains the stable mapping model, so RitsuLib can manage targeting,
/// queueing and cancellation. Resource spending is nevertheless performed by the real card so
/// X values, temporary costs and resource-spent hooks receive the correct source model.
/// </summary>
public sealed class RecordMappingSpendResourcesPatch : IPatchMethod
{
    public static string PatchId => "janus_record_mapping_spend_resources";
    public static string Description => "Spend Record mapping resources through its Diary original";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CardModel), nameof(CardModel.SpendResources), Type.EmptyTypes)];
    }

    [HarmonyPrefix]
    public static bool Prefix(CardModel __instance, ref Task<(int, int)> __result)
    {
        if (__instance is not JanusRecordMappingCard mapping ||
            RecordExtraHandManager.ResolveOriginal(mapping) is not { CanTake: true } original ||
            original.Pile?.Type != MainFile.Diary ||
            !ReferenceEquals(original.Owner, mapping.Owner))
        {
            return true;
        }

        __result = original.SpendResources();
        return false;
    }
}

/// <summary>
/// At the normal CardModel.OnPlayWrapper boundary, replace the presentation model with the Diary
/// original. The official wrapper then owns the play pile, hooks, history and result location.
/// </summary>
public sealed class RecordMappingPlayBridgePatch : IPatchMethod
{
    public static string PatchId => "janus_record_mapping_play_bridge";
    public static string Description => "Resolve a played Record mapping as its Diary original";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(CardModel),
                nameof(CardModel.OnPlayWrapper),
                [
                    typeof(PlayerChoiceContext),
                    typeof(Creature),
                    typeof(bool),
                    typeof(ResourceInfo),
                    typeof(bool)
                ])
        ];
    }

    [HarmonyPrefix]
    public static bool Prefix(
        CardModel __instance,
        PlayerChoiceContext choiceContext,
        Creature? target,
        bool isAutoPlay,
        ResourceInfo resources,
        bool skipCardPileVisuals,
        ref Task __result)
    {
        if (__instance is not JanusRecordMappingCard mapping)
        {
            return true;
        }

        __result = PlayOriginal(
            mapping,
            choiceContext,
            target,
            isAutoPlay,
            resources,
            skipCardPileVisuals);
        return false;
    }

    private static async Task PlayOriginal(
        JanusRecordMappingCard mapping,
        PlayerChoiceContext choiceContext,
        Creature? target,
        bool isAutoPlay,
        ResourceInfo resources,
        bool skipCardPileVisuals)
    {
        JanusRecordCardModel? original = RecordExtraHandManager.ResolveOriginal(mapping);
        if (original is not { CanTake: true } ||
            original.Pile?.Type != MainFile.Diary ||
            !ReferenceEquals(original.Owner, mapping.Owner))
        {
            return;
        }

        // RitsuLib has already released the extra-hand holder to the official play queue.
        // Rebind that exact visual before changing either model's pile.
        if (NCardPlayQueue.Instance?.GetCardNode(mapping) is { } queuedCard)
        {
            queuedCard.Model = original;
        }

        RecordExtraHandManager.ParkMappingForPlay(mapping);

        // Notify the Diary pile so its visible count is updated immediately. The temporary Hand
        // insertion stays silent because the queued NCard already represents the original.
        original.Pile.RemoveInternal(original);
        PileType.Hand.GetPile(original.Owner).AddInternal(original, -1, true);

        await original.OnPlayWrapper(
            choiceContext,
            target,
            isAutoPlay,
            resources,
            skipCardPileVisuals);
    }
}

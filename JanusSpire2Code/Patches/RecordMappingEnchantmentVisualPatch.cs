using System;
using Godot;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Cards;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

/// <summary>
/// A mapping's base description already contains the Diary original's fully formatted text.
/// Returning that text directly prevents the mapping's own description pipeline from appending
/// enchantment or affliction extra text a second time.
/// </summary>
public sealed class RecordMappingDescriptionPatch : IPatchMethod
{
    public static string PatchId => "janus_record_mapping_description";

    public static string Description =>
        "Render a Record mapping with exactly its Diary original's formatted description";

    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(
            typeof(CardModel),
            nameof(CardModel.GetDescriptionForPile),
            [typeof(PileType), typeof(Creature)])
    ];

    [HarmonyPrefix]
    public static bool Prefix(
        CardModel __instance,
        Creature? target,
        ref string __result)
    {
        if (__instance is not JanusRecordMappingCard mapping)
        {
            return true;
        }

        JanusRecordCardModel? original = RecordExtraHandManager.ResolveOriginal(mapping);
        __result = original?.GetDescriptionForPile(PileType.Hand, target) ?? string.Empty;
        return false;
    }
}

/// <summary>
/// NCard normally updates the dynamic variables owned by its displayed model. A Record mapping
/// deliberately owns no gameplay variables, so update the Diary original's variables with the
/// same preview mode and target instead. The original remains in the Diary; running each official
/// DynamicVar preview with hand-equivalent global hooks avoids changing any synchronized state.
/// </summary>
public sealed class RecordMappingDynamicVarPreviewPatch : IPatchMethod
{
    public static string PatchId => "janus_record_mapping_dynamic_var_preview";

    public static string Description =>
        "Calculate Record mapping values from the Diary original with hand preview semantics";

    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(
            typeof(CardModel),
            nameof(CardModel.UpdateDynamicVarPreview),
            [typeof(CardPreviewMode), typeof(Creature), typeof(DynamicVarSet)])
    ];

    [HarmonyPostfix]
    public static void Postfix(
        CardModel __instance,
        CardPreviewMode previewMode,
        Creature? target,
        DynamicVarSet dynamicVarSet)
    {
        if (__instance is not JanusRecordMappingCard mapping ||
            !ReferenceEquals(dynamicVarSet, mapping.DynamicVars) ||
            RecordExtraHandManager.ResolveOriginal(mapping) is not { } original)
        {
            return;
        }

        UpdatePreviewSet(original, original.DynamicVars, previewMode, target);
        if (original.Enchantment is { } enchantment)
        {
            UpdatePreviewSet(original, enchantment.DynamicVars, previewMode, target);
        }
    }

    internal static void ClearOriginalPreview(JanusRecordMappingCard mapping)
    {
        JanusRecordCardModel? original = RecordExtraHandManager.ResolveOriginal(mapping);
        original?.DynamicVars.ClearPreview();
        original?.Enchantment?.DynamicVars.ClearPreview();
    }

    private static void UpdatePreviewSet(
        JanusRecordCardModel original,
        DynamicVarSet variables,
        CardPreviewMode previewMode,
        Creature? target)
    {
        variables.ClearPreview();
        foreach (DynamicVar variable in variables.Values.ToList())
        {
            variable.UpdateCardPreview(original, previewMode, target, true);
        }
    }
}

/// <summary>
/// Preserve NCard's unpowered-preview behavior. Its normal reset only touches the mapping's empty
/// variable set, so reset the delegated original before every mapping visual refresh as well.
/// </summary>
public sealed class RecordMappingDynamicVarPreviewResetPatch : IPatchMethod
{
    public static string PatchId => "janus_record_mapping_dynamic_var_preview_reset";

    public static string Description => "Reset delegated Record preview values before rendering";

    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(
            typeof(NCard),
            nameof(NCard.UpdateVisuals),
            [typeof(PileType), typeof(CardPreviewMode)])
    ];

    [HarmonyPrefix]
    public static void Prefix(NCard __instance)
    {
        if (__instance.Model is JanusRecordMappingCard mapping)
        {
            RecordMappingDynamicVarPreviewPatch.ClearOriginalPreview(mapping);
        }
    }
}

/// <summary>
/// Keeps Record mappings presentation-only while letting the vanilla card node render the
/// Diary original's enchantment icon, amount, and active/disabled state.
/// </summary>
public sealed class RecordMappingEnchantmentVisualPatch : IPatchMethod
{
    private static readonly Action<NCard, EnchantmentStatus>? SetEnchantmentStatus =
        AccessTools.Method(
                typeof(NCard),
                "SetEnchantmentStatus",
                [typeof(EnchantmentStatus)])
            ?.CreateDelegate<Action<NCard, EnchantmentStatus>>();

    public static string PatchId => "janus_record_mapping_enchantment_visual";

    public static string Description =>
        "Render a Record mapping with its Diary original's enchantment visual state";

    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCard), "UpdateEnchantmentVisuals", Type.EmptyTypes)
    ];

    [HarmonyPostfix]
    public static void Postfix(NCard __instance)
    {
        if (__instance.Model is not JanusRecordMappingCard mapping)
        {
            return;
        }

        EnchantmentModel? enchantment = RecordExtraHandManager.ResolveOriginal(mapping)?.Enchantment;
        if (enchantment == null)
        {
            __instance.EnchantmentTab.Visible = false;
            return;
        }

        TextureRect icon = __instance.GetNode<TextureRect>("%Enchantment/Icon");
        MegaLabel label = __instance.GetNode<MegaLabel>("%Enchantment/Label");

        __instance.EnchantmentTab.Visible = true;
        icon.Texture = enchantment.Icon;
        label.SetTextAutoSize(enchantment.DisplayAmount.ToString());
        label.Visible = enchantment.ShowAmount;
        SetEnchantmentStatus?.Invoke(__instance, enchantment.Status);
    }
}

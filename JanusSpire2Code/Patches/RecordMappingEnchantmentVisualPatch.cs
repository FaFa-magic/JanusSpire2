using System;
using Godot;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Cards;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
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

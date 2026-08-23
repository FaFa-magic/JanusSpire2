using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class SwiftStatusAndCurseEnchantPatch : IPatchMethod
{
    public static string PatchId => "janus_swift_status_and_curse_enchant";

    public static bool IsCritical => false;

    public static string Description =>
        "Allow Swift to enchant Status and Curse cards and stack on existing Swift cards in combat";

    public static ModPatchTarget[] GetTargets() =>
    [
        new(
            typeof(EnchantmentModel),
            nameof(EnchantmentModel.CanEnchant),
            [typeof(CardModel)])
    ];

    [HarmonyPostfix]
    public static void Postfix(EnchantmentModel __instance, CardModel card, ref bool __result)
    {
        if (__result ||
            __instance is not Swift ||
            card.CombatState == null ||
            !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        if (card.Enchantment is Swift)
        {
            __result = true;
            return;
        }

        if (card.Type != CardType.Status &&
            card.Type != CardType.Curse &&
            card.Type != CardType.Quest)
        {
            return;
        }

        // Only bypass the base Status/Curse type gate. Preserve every other
        // enchantment rule, especially deck restrictions and enchant conflicts.
        if (!__instance.CanEnchantCardType(card.Type) ||
            (card.Pile?.Type == PileType.Deck && card.Keywords.Contains(CardKeyword.Unplayable)) ||
            (card.Enchantment != null &&
             (!__instance.IsStackable || card.Enchantment.GetType() != __instance.GetType())))
        {
            return;
        }

        __result = true;
    }
}

public sealed class SwiftCombatStackVisualRefreshPatch : IPatchMethod
{
    public static string PatchId => "janus_swift_combat_stack_visual_refresh";

    public static bool IsCritical => false;

    public static string Description => "Refresh a combat card after applying or stacking Swift";

    public static ModPatchTarget[] GetTargets() =>
    [
        new(
            typeof(CardCmd),
            nameof(CardCmd.Enchant),
            [typeof(EnchantmentModel), typeof(CardModel), typeof(decimal)])
    ];

    [HarmonyPostfix]
    public static void Postfix(CardModel card, EnchantmentModel? __result)
    {
        if (__result is Swift &&
            card.CombatState != null &&
            CombatManager.Instance.IsInProgress)
        {
            card.RequestVisualReload();
        }
    }
}

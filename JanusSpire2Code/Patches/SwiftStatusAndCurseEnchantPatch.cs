using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class SwiftStatusAndCurseEnchantPatch : IPatchMethod
{
    public static string PatchId => "janus_swift_status_and_curse_enchant";

    public static bool IsCritical => false;

    public static string Description => "Allow Swift to enchant Status and Curse cards in combat";

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
            (card.Type != CardType.Status && card.Type != CardType.Curse && card.Type != CardType.Quest))
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

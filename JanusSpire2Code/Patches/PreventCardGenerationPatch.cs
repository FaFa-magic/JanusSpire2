using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class PreventSingleCardGenerationPatch : IPatchMethod
{
    public static string PatchId => "PreventSingleCardGenerationPatch";
    public static string Description => "PreventSingleCardGenerationPatch";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardToCombat), [typeof(CardModel), typeof(PileType), typeof(Player), typeof(CardPilePosition)])
    ];

    [HarmonyPrefix]
    public static bool Prefix(CardModel card, ref Task<CardPileAddResult> __result)
    {
        if (card.Owner.Creature.GetPower<MidsummerHolidayPower>() == null)
            return true;

        __result = Task.FromResult(default(CardPileAddResult));
        return false;
    }
}

public sealed class PreventMultipleCardGenerationPatch : IPatchMethod
{
    public static string PatchId => "PreventMultipleCardGenerationPatch";
    public static string Description => "PreventMultipleCardGenerationPatch";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardsToCombat), [typeof(IEnumerable<CardModel>), typeof(PileType), typeof(Player), typeof(CardPilePosition)])
    ];

    [HarmonyPrefix]
    public static bool Prefix(ref IEnumerable<CardModel> cards, ref Task<IReadOnlyList<CardPileAddResult>> __result)
    {
        CardModel[] allowedCards = cards.Where(card => card.Owner.Creature.GetPower<MidsummerHolidayPower>() == null).ToArray();

        if (allowedCards.Length > 0)
        {
            cards = allowedCards;
            return true;
        }

        __result = Task.FromResult<IReadOnlyList<CardPileAddResult>>(Array.Empty<CardPileAddResult>());
        return false;
    }
}
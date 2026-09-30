using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using System.Runtime.CompilerServices;

namespace JanusSpire2.JanusSpire2Code.Patches;

internal static class MidsummerRejectedGeneration
{
    private static readonly ConditionalWeakTable<CardModel, object> RejectedCards = new();

    internal static void Mark(CardModel card) => RejectedCards.GetValue(card, static _ => new object());

    internal static void Clear(CardModel card) => RejectedCards.Remove(card);

    internal static bool ShouldBlockAutoPlay(CardModel card)
    {
        if (!RejectedCards.TryGetValue(card, out _))
            return false;
        if (card.Pile != null)
        {
            RejectedCards.Remove(card);
            return false;
        }
        return true;
    }
}

public sealed class PreventMultipleCardGenerationPatch : IPatchMethod
{
    internal sealed record GenerationBatchState(CardModel[] Cards, bool[] Allowed, PileType TargetPile);

    public static string PatchId => "PreventMultipleCardGenerationPatch";
    public static string Description => "PreventMultipleCardGenerationPatch";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardsToCombat), [typeof(IEnumerable<CardModel>), typeof(PileType), typeof(Player), typeof(CardPilePosition)])
    ];

    [HarmonyPrefix]
    internal static bool Prefix(ref IEnumerable<CardModel> cards, PileType newPileType,
        ref Task<IReadOnlyList<CardPileAddResult>> __result, out GenerationBatchState? __state)
    {
        __state = null;
        CardModel[] requestedCards = cards.ToArray();
        bool[] allowed = requestedCards
            .Select(card => card.Owner.Creature.GetPower<MidsummerHolidayPower>() == null)
            .ToArray();

        for (int i = 0; i < requestedCards.Length; i++)
        {
            if (allowed[i])
                MidsummerRejectedGeneration.Clear(requestedCards[i]);
            else
                MidsummerRejectedGeneration.Mark(requestedCards[i]);
        }

        if (allowed.All(value => value))
        {
            cards = requestedCards;
            return true;
        }

        if (allowed.Any(value => value))
        {
            __state = new GenerationBatchState(requestedCards, allowed, newPileType);
            cards = requestedCards.Where((_, index) => allowed[index]).ToArray();
            return true;
        }

        __result = Task.FromResult<IReadOnlyList<CardPileAddResult>>(
            requestedCards.Select(card => BlockedResult(card, newPileType)).ToArray());
        return false;
    }

    [HarmonyPostfix]
    internal static void Postfix(ref Task<IReadOnlyList<CardPileAddResult>> __result,
        GenerationBatchState? __state)
    {
        if (__state != null)
            __result = RestoreOriginalOrder(__result, __state);
    }

    private static async Task<IReadOnlyList<CardPileAddResult>> RestoreOriginalOrder(
        Task<IReadOnlyList<CardPileAddResult>> originalTask, GenerationBatchState state)
    {
        IReadOnlyList<CardPileAddResult> allowedResults = await originalTask;
        CardPileAddResult[] results = new CardPileAddResult[state.Cards.Length];
        int allowedIndex = 0;
        for (int i = 0; i < results.Length; i++)
        {
            results[i] = state.Allowed[i] && allowedIndex < allowedResults.Count
                ? allowedResults[allowedIndex++]
                : BlockedResult(state.Cards[i], state.TargetPile);
        }

        return results;
    }

    private static CardPileAddResult BlockedResult(CardModel card, PileType targetPile) => new()
    {
        cardAdded = card,
        targetPile = targetPile,
        success = false
    };
}

public sealed class PreventRejectedGeneratedCardAutoPlayPatch : IPatchMethod
{
    public static string PatchId => "PreventRejectedGeneratedCardAutoPlayPatch";
    public static string Description => "Prevent rejected generated cards from being auto-played outside a pile";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CardCmd), nameof(CardCmd.AutoPlay),
            [typeof(PlayerChoiceContext), typeof(CardModel), typeof(Creature), typeof(AutoPlayType), typeof(bool), typeof(bool)])
    ];

    [HarmonyPrefix]
    internal static bool Prefix(CardModel card, ref Task __result)
    {
        if (!MidsummerRejectedGeneration.ShouldBlockAutoPlay(card))
            return true;
        __result = Task.CompletedTask;
        return false;
    }
}

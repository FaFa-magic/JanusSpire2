using Godot;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

internal static class InspirationPileGlow
{
    internal static bool IsCombatPileGrid(NCardGrid grid)
    {
        for (Node? node = grid; node != null; node = node.GetParent())
        {
            if (node is NCardPileScreen)
            {
                return true;
            }
        }

        return false;
    }

    internal static void Refresh(NGridCardHolder holder)
    {
        if (!holder.Visible ||
            holder.CardNode?.Model is not { } card ||
            !InspirationKeyword.ShouldGlow(card))
        {
            return;
        }

        holder.CardNode.CardHighlight.Modulate = NCardHighlight.playableColor;
        holder.CardNode.CardHighlight.AnimShow();
    }
}

/// <summary>
/// NCardGrid creates the initially visible cards directly in InitGrid; it does not pass them
/// through AssignCardsToRow until a row is recycled by scrolling.
/// </summary>
public sealed class InspirationPileGlowInitPatch : IPatchMethod
{
    public static string PatchId => "janus_inspiration_pile_glow_init";

    public static string Description =>
        "Show Inspiration glow when a pile grid initially creates its card nodes";

    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NCardGrid), "InitGrid", Type.EmptyTypes)
    ];

    [HarmonyPostfix]
    public static void Postfix(NCardGrid __instance)
    {
        if (!InspirationPileGlow.IsCombatPileGrid(__instance))
        {
            return;
        }

        foreach (NGridCardHolder holder in __instance.CurrentlyDisplayedCardHolders)
        {
            InspirationPileGlow.Refresh(holder);
        }
    }
}

/// <summary>
/// The built-in hand glow registries only affect NHandCardHolder. Inspiration cards are
/// activated from pile grids, so apply the game's normal playable-blue highlight after the
/// grid finishes assigning its cards (the vanilla method hides every non-selected highlight).
/// </summary>
public sealed class InspirationPileGlowPatch : IPatchMethod
{
    public static string PatchId => "janus_inspiration_pile_glow";

    public static string Description =>
        "Show the vanilla playable-blue highlight on usable Inspiration cards outside the hand";

    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(
            typeof(NCardGrid),
            "AssignCardsToRow",
            [typeof(List<NGridCardHolder>), typeof(int)])
    ];

    [HarmonyPostfix]
    public static void Postfix(NCardGrid __instance, List<NGridCardHolder> row)
    {
        if (!InspirationPileGlow.IsCombatPileGrid(__instance))
        {
            return;
        }

        foreach (NGridCardHolder holder in row)
        {
            InspirationPileGlow.Refresh(holder);
        }
    }
}

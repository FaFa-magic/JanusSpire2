using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class PerkDiarySelectionRightClickPatch : IPatchMethod
{
    public static string PatchId => "janus_perk_diary_selection_right_click";

    public static bool IsCritical => false;

    public static string Description => "Guard Perk cost selection and use displayed combat-pile cards for inspection";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(NCardGridSelectionScreen),
                "ShowCardDetail",
                [typeof(CardModel)])
        ];
    }

    public static bool Prefix(NCardGridSelectionScreen __instance, CardModel card,
        NCardGrid ____grid, ref IReadOnlyList<CardModel> ____cards)
    {
        if (DiaryOnPlayWrapperPatch.IsSelectingPerkCostFromDiary(card))
            return false;

        if (__instance is NCombatPileCardSelectScreen)
        {
            // The combat-pile screen updates its grid, but leaves the base _cards
            // array empty. Use the actually displayed (filtered/sorted) cards.
            ____cards = ____grid.CurrentlyDisplayedCards.ToList();
            if (!____cards.Contains(card))
                return false;
        }

        return true;
    }
}

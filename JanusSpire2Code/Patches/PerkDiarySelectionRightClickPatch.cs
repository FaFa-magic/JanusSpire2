using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class PerkDiarySelectionRightClickPatch : IPatchMethod
{
    public static string PatchId => "janus_perk_diary_selection_right_click";

    public static bool IsCritical => false;

    public static string Description => "Disable card inspection while selecting Diary cards to pay a Perk cost";

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

    public static bool Prefix(CardModel card)
    {
        return !DiaryOnPlayWrapperPatch.IsSelectingPerkCostFromDiary(card);
    }
}

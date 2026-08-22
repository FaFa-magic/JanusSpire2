using JanusSpire2.JanusSpire2Code.Cards;
using JanusSpire2.JanusSpire2Code.Cards.Uncommon;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class HolyNightDiaryLocationRecoveryPatch : IPatchMethod
{
    public static string PatchId => "janus_holy_night_diary_location_recovery";

    public static bool IsCritical => false;

    public static string Description => "Recover supported cards in the dynamic Diary pile after right-click serialization";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(ModRightClickRegistry),
                "IsCardInExpectedLocation",
                [typeof(CardModel), typeof(ModRightClickTrigger)])
        ];
    }

    public static void Postfix(
        CardModel card,
        ModRightClickTrigger trigger,
        ref bool __result)
    {
        if (__result ||
            card.Pile?.Type != MainFile.Diary ||
            trigger.Source != ModRightClickSource.CombatPileCard ||
            (card is not HolyNight && card is not JanusRecordCardModel))
        {
            return;
        }

        __result = true;
    }
}

using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

/// <summary>
/// Makes Tender's paired Strength/Dexterity loss atomic for Dazzling.
/// Skipping Tender's original hook is important: its counter is the amount it
/// restores later, so incrementing it after preventing the loss would grant
/// permanent positive stats at turn end.
/// </summary>
public sealed class DazzlingTenderPatch : IPatchMethod
{
    public static string PatchId => "janus_dazzling_tender_atomic_conversion";

    public static string Description => "Convert one complete Tender penalty with Dazzling";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(TenderPower),
                nameof(TenderPower.AfterCardPlayed),
                [typeof(PlayerChoiceContext), typeof(CardPlay)])
        ];
    }

    [HarmonyPrefix]
    public static bool Prefix(
        TenderPower __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result)
    {
        if (cardPlay.Card.Owner != __instance.Owner.Player)
        {
            return true;
        }

        DazzlingPower? dazzling = __instance.Owner.GetPower<DazzlingPower>();
        if (dazzling == null || dazzling.Amount <= 0)
        {
            return true;
        }

        __result = dazzling.ConvertTenderPenalty(choiceContext);
        return false;
    }
}

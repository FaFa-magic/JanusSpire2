using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

/// <summary>
/// Disables Tender outside its owner's player turn and makes its paired
/// Strength/Dexterity loss atomic for Dazzling. Skipping Tender's original hook
/// is important: its counter is the amount it restores later, so incrementing
/// it without applying the complete penalty would grant permanent positive stats.
/// </summary>
public sealed class DazzlingTenderPatch : IPatchMethod
{
    public static string PatchId => "janus_dazzling_tender_atomic_conversion";

    public static string Description =>
        "Disable Tender outside its owner's turn and convert its complete penalty with Dazzling";

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

        if (!CombatManager.Instance.IsPartOfPlayerTurn(cardPlay.Player))
        {
            __result = Task.CompletedTask;
            return false;
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

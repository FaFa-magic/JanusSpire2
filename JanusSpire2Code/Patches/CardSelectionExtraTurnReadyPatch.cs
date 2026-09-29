using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class CardSelectionExtraTurnReadyPatch : IPatchMethod
{
    public static string PatchId => "janus_card_selection_extra_turn_ready";
    public static string Description => "Preserve end-turn readiness when a choosing player is not participating in the current turn";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [new(typeof(CardSelectCmd), "UndoEndTurnIfNecessary", [typeof(Player)])];

    [HarmonyPrefix]
    public static bool Prefix(Player player)
    {
        // The official selector checks only CurrentSide. An extra turn also has
        // non-participants, whose automatic readiness must remain intact.
        // Selection and its synchronized choice context continue unchanged.
        return CombatManager.Instance.IsPartOfPlayerTurn(player);
    }
}

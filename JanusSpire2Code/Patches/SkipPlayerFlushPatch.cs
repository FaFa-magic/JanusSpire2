using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class SkipPlayerFlushPatch : IPatchMethod
{
    public static string PatchId => "SkipPlayerFlushPatch";
    public static string Description => "Skips hand flush for players with ISkipPlayerFlushRelic during their turn.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CombatManager), "FlushPlayerHand")];

    [HarmonyPrefix]
    public static bool Prefix(ref Task __result, Player player)
    {
        CombatState? currentState = CombatManager.Instance.DebugOnlyGetState();

        bool hasSkipRelic = player.Relics != null && player.Relics.Any(r => r is ISkipPlayerFlushRelic); 

        if (hasSkipRelic && currentState != null)
        {
            if (currentState.CurrentSide == CombatSide.Player)
            {
                __result = Task.CompletedTask;
                return false; 
            }
        }
        
        return true; 
    }
}

public interface ISkipPlayerFlushRelic
{
}
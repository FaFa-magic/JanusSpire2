using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class EnemyTurnFlushPatch : IPatchMethod
{
    public static string PatchId => "EnemyTurnFlushPatch";
    public static string Description => "Executes hand flush for ISkipPlayerFlushRelic players at the end of enemy turn.";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CombatManager), "EndEnemyTurnInternal")];

    [HarmonyPostfix]
    public static void Postfix(ref Task __result, CombatManager __instance, object[] __args)
    {
        __result = PostfixWrapper(__result, __instance, __args);
    }

    private static async Task PostfixWrapper(Task originalTask, CombatManager instance, object[] args)
    {
        if (originalTask != null)
        {
            await originalTask;
        }
        
        CombatState? currentState = instance.DebugOnlyGetState();

        if (currentState == null || !LocalContext.NetId.HasValue) return;

        object turnState = args[0];

        MethodInfo? flushMethod = AccessTools.Method(typeof(CombatManager), "FlushPlayerHand");

        foreach (var player in currentState.Players)
        {
            bool hasSkipRelic = player.Relics != null && player.Relics.Any(r => r is ISkipPlayerFlushRelic); 
            
            if (hasSkipRelic && flushMethod != null)
            {
                HookPlayerChoiceContext playerChoiceContext = new HookPlayerChoiceContext(player, LocalContext.NetId.Value, GameActionType.CombatPlayPhaseOnly);

                Task? flushTask = flushMethod.Invoke(instance, new object[] { turnState, player, playerChoiceContext }) as Task;

                if (flushTask != null)
                {
                    await playerChoiceContext.AssignTaskAndWaitForPauseOrCompletion(flushTask);
                    await playerChoiceContext.WaitForCompletion();
                }
            }
        }
    }
}
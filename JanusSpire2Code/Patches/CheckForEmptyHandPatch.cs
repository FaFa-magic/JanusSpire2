using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class CheckForEmptyHandPatch : IPatchMethod
{
    public static string PatchId => "CheckForEmptyHandPatch";

    public static string Description => "CheckForEmptyHandPatch";
    
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CombatManager), nameof(CombatManager.CheckForEmptyHand))];

    [HarmonyPostfix]
    public static void Postfix(ref Task __result, PlayerChoiceContext choiceContext, Player player)
    {
        __result = PostfixWrapper(__result, choiceContext, player);
    }

    private static async Task PostfixWrapper(Task originalTask, PlayerChoiceContext choiceContext, Player player)
    {
        await originalTask;

        if (player.Creature?.Powers != null)
        {
            foreach (var power in player.Creature.Powers.ToList())
            {
                if (power is IAfterHandReducedHook customPower)
                {
                    await customPower.AfterHandReduced(choiceContext, player);
                }
            }
        }
    }
}

public interface IAfterHandReducedHook
{
    Task AfterHandReduced(PlayerChoiceContext choiceContext, Player player);
}
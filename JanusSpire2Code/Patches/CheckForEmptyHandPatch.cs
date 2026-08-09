using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
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
    [
        new(
            typeof(CombatManager),
            nameof(CombatManager.CheckForEmptyHand),
            new[]
            {
                AccessTools.TypeByName("CombatTurnState"),
                typeof(PlayerChoiceContext),
                typeof(Player)
            })
    ];

    [HarmonyPrefix]
    public static bool Prefix(ref Task __result, CombatManager __instance, object __0, PlayerChoiceContext choiceContext, Player player)
    {
        __result = PostfixWrapper(__instance, __0, choiceContext, player);
        return false;
    }

    private static async Task PostfixWrapper(CombatManager instance, object turnState, PlayerChoiceContext choiceContext, Player player)
    {
        bool isInProgress = (bool?)AccessTools.Property(turnState.GetType(), "IsInProgress")?.GetValue(turnState) ?? false;

        bool isExecuting = instance.IsExecutingCardOrPotionEffect(player);

        if (!isInProgress || isExecuting)
            return;

        int handCount = PileType.Hand.GetPile(player).Cards.Count;

        if (player.Creature?.Powers == null)
            return;

        int threshold = 0;
        List<IAfterHandReducedHook> customHooks = new();

        foreach (var power in player.Creature.Powers.ToList())
        {
            if (power is IAfterHandReducedHook customPower)
            {
                threshold += power.Amount;
                customHooks.Add(customPower);
            }
        }

        if (threshold > 0 && handCount < threshold)
        {
            foreach (var customHook in customHooks)
            {
                await customHook.AfterHandReduced(choiceContext, player);
            }
        }
    }
}

public interface IAfterHandReducedHook
{
    Task AfterHandReduced(PlayerChoiceContext choiceContext, Player player);
}
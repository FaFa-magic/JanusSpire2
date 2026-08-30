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

    [HarmonyPostfix]
    public static void Postfix(ref Task __result, CombatManager __instance, object __0, PlayerChoiceContext choiceContext, Player player)
    {
        __result = AfterVanillaCheck(__result, __instance, __0, choiceContext, player);
    }

    private static async Task AfterVanillaCheck(
        Task vanillaTask,
        CombatManager instance,
        object turnState,
        PlayerChoiceContext choiceContext,
        Player player)
    {
        // Preserve the official empty-hand hook and RitsuLib 0.5.18's play-enabled extra-hand
        // query. Confidence's reduced-hand threshold remains based on the real vanilla hand, so
        // presentation-only Record mappings do not become gameplay card-counting objects.
        await vanillaTask;

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

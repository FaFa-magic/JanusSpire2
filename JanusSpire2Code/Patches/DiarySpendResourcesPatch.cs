using System.Reflection;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class DiarySpendResourcesPatch : IPatchMethod
{
    public static string PatchId => "DiarySpendResourcesPatch";
    public static string Description => "DiarySpendResourcesPatch";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(CardModel), nameof(CardModel.SpendResources))];

    [HarmonyPrefix]
    public static bool Prefix(CardModel __instance, ref Task<(int, int)> __result)
    {
        if (!__instance.Keywords.Contains(JanusKeywords.Perk) || __instance.Owner?.PlayerCombatState == null)
        {
            return true;
        }

        __result = SpendResourcesWithDiarySubstitution(__instance);
        return false;
    }

    private static async Task<(int, int)> SpendResourcesWithDiarySubstitution(CardModel card)
    {
        PlayerCombatState state = card.Owner!.PlayerCombatState!;
        int cost = card.EnergyCost.GetAmountToSpend();
        int energyToSpend = Math.Min(cost, state.Energy);

        MethodInfo? spendEnergyMethod = AccessTools.DeclaredMethod(typeof(CardModel), "SpendEnergy", new Type[] { typeof(int) });
        if (spendEnergyMethod != null && spendEnergyMethod.Invoke(card, new object[] { energyToSpend }) is Task energyTask)
        {
            await energyTask;
        }

        int starsToSpend = 0;
        var starCostProp = AccessTools.Property(typeof(CardModel), "StarCost") 
                        ?? AccessTools.Property(typeof(CardModel), "StarsCost");
        var starCostField = AccessTools.Field(typeof(CardModel), "StarCost") 
                         ?? AccessTools.Field(typeof(CardModel), "StarsCost");

        object? starCostObj = starCostProp?.GetValue(card) ?? starCostField?.GetValue(card);
        if (starCostObj != null)
        {
            var getAmountMethod = AccessTools.Method(starCostObj.GetType(), "GetAmountToSpend");
            if (getAmountMethod != null)
            {
                starsToSpend = (int)(getAmountMethod.Invoke(starCostObj, null) ?? 0);
            }
        }

        if (starsToSpend > 0)
        {
            MethodInfo? spendStarsMethod = AccessTools.Method(typeof(CardModel), "SpendStars", new Type[] { typeof(int) }) 
                                        ?? AccessTools.Method(typeof(CardModel), "SpendStar", new Type[] { typeof(int) });
            if (spendStarsMethod != null && spendStarsMethod.Invoke(card, new object[] { starsToSpend }) is Task starTask)
            {
                await starTask;
            }
        }

        return (energyToSpend, starsToSpend);
    }
}
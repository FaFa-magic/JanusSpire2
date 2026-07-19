using System.Reflection;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
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
        PlayerCombatState? playerCombatState = card.Owner?.PlayerCombatState;
        if (playerCombatState == null)
        {
            return (0, 0);
        }

        var diaryPile = playerCombatState.AllPiles?.FirstOrDefault(p => p.Type == MainFile.Diary);
            
        int cost = card.EnergyCost.GetAmountToSpend();
        int energy = playerCombatState.Energy; 
        int minSelect = Math.Max(0, cost - energy);
        int maxSelect = cost;
        int diarySpent = 0;

        if (diaryPile != null && diaryPile.Cards.Count > 0 && maxSelect > 0)
        {
            List<CardModel> cardsInDiary = diaryPile.Cards.ToList();
            LocString promptText = new LocString("ui", "DiarySelectionPrompt");
            
            promptText.Add("Min", (decimal)minSelect); 
            promptText.Add("Max", (decimal)maxSelect);
            
            var prefs = new CardSelectorPrefs(promptText, minSelect, maxSelect);

            var selected = (await CardSelectCmd.FromSimpleGrid(
                null!, 
                cardsInDiary, 
                card.Owner!, 
                prefs
            )).ToList();

            diarySpent = selected.Count;

            foreach (var item in selected)
            {
                await CardCmd.Exhaust(null!, item);
            }
        }

        int energyToSpend = Math.Max(0, cost - diarySpent);

        MethodInfo? spendEnergyMethod = AccessTools.DeclaredMethod(typeof(CardModel), "SpendEnergy", new Type[] { typeof(int) });
        if (spendEnergyMethod != null)
        {
            if (spendEnergyMethod.Invoke(card, new object[] { energyToSpend }) is Task energyTask)
            {
                await energyTask;
            }
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
            if (spendStarsMethod != null)
            {
                if (spendStarsMethod.Invoke(card, new object[] { starsToSpend }) is Task starTask)
                {
                    await starTask;
                }
            }
        }

        return (energyToSpend, starsToSpend);
    }
}
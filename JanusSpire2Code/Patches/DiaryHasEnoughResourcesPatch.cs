using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class DiaryHasEnoughResourcesPatch : IPatchMethod
{
    public static string PatchId => "DiaryHasEnoughResourcesPatch";
    public static string Description => "DiaryHasEnoughResourcesPatch";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(PlayerCombatState), nameof(PlayerCombatState.HasEnoughResourcesFor))];

    [HarmonyPostfix]
    public static void Postfix(PlayerCombatState __instance, CardModel card, ref UnplayableReason reason, ref bool __result)
    {
        if (!__result && card.Keywords.Contains(JanusKeywords.Perk))
        {
            UnplayableReason energyReason = (UnplayableReason)16;
            
            if ((reason & energyReason) != 0)
            {
                int cost = Math.Max(0, card.EnergyCost.GetWithModifiers((CostModifiers)(-1)));
                int energy = __instance.Energy;

                var diaryPile = __instance.AllPiles?.FirstOrDefault(p => p.Type == MainFile.Diary);
                    
                if (cost <= energy || (diaryPile != null && diaryPile.Cards.Count + energy >= cost))
                {
                    reason &= ~energyReason;
                    
                    if (reason == UnplayableReason.None)
                    {
                        __result = true;
                    }
                }
            }
        }
    }
}
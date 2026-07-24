using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class DiaryOnPlayWrapperPatch : IPatchMethod
{
    public static string PatchId => "DiaryOnPlayWrapperPatch";
    public static string Description => "DiaryOnPlayWrapperPatch";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => [new(typeof(CardModel), nameof(CardModel.OnPlayWrapper))];

    private static readonly HashSet<CardModel> _processingCards = new();

    [HarmonyPrefix]
    public static bool Prefix(CardModel __instance, PlayerChoiceContext choiceContext, Creature? target, bool isAutoPlay, ResourceInfo resources, bool skipCardPileVisuals, ref Task __result)
    {
        if (_processingCards.Contains(__instance) || !__instance.Keywords.Contains(JanusKeywords.Perk))
        {
            return true; 
        }

        __result = CustomSafeWrapper(__instance, choiceContext, target, isAutoPlay, resources, skipCardPileVisuals);

        return false; 
    }

    private static async Task CustomSafeWrapper(CardModel card, PlayerChoiceContext choiceContext, Creature? target, bool isAutoPlay, ResourceInfo resources, bool skipCardPileVisuals)
    {
        try
        {
            _processingCards.Add(card);

            PlayerCombatState? playerCombatState = card.Owner?.PlayerCombatState;
            if (playerCombatState != null)
            {
                int cost = card.EnergyCost.GetAmountToSpend();
                int need = Math.Max(0, cost - resources.EnergySpent);
                
                var diaryPile = playerCombatState.AllPiles?.FirstOrDefault(p => p.Type == MainFile.Diary);

                if (need > 0 && diaryPile != null && diaryPile.Cards.Count >= need)
                {
                    var selected = (await CardSelectCmd.FromCombatPile(
                        choiceContext, 
                        diaryPile, 
                        card.Owner!, 
                        new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, need))).ToList();
                    
                    foreach (var item in selected)
                    {
                        await CardCmd.Exhaust(choiceContext, item);
                    }
                }
            }

            await card.OnPlayWrapper(choiceContext, target, isAutoPlay, resources, skipCardPileVisuals);
        }
        finally
        {
            _processingCards.Remove(card);
        }
    }
}
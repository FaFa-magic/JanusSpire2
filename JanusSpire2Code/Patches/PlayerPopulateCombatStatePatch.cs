using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class PlayerPopulateCombatStatePatch : IPatchMethod
{
    public static string PatchId => "PlayerPopulateCombatStatePatch";

    public static string Description => "PlayerPopulateCombatStatePatch";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() => [new(typeof(Player), nameof(Player.PopulateCombatState))];

    [HarmonyPrefix]
    public static bool Prefix(Player __instance, Rng rng, CombatState state)
    {
        foreach (CardModel deckCard in __instance.Deck.Cards.ToList())
        {
            CardModel combatCard = state.CloneCard(deckCard);
            combatCard.DeckVersion = deckCard;

            if (combatCard.Keywords.Contains(JanusKeywords.Collection))
            {
                MainFile.Diary.GetPile(__instance).AddInternal(combatCard);
            }
            else
            {
                __instance.PlayerCombatState!.DrawPile.AddInternal(combatCard);
            }
        }

        __instance.PlayerCombatState!.DrawPile.RandomizeOrderInternal(
            __instance,
            rng,
            state
        );

        return false;
    }
}
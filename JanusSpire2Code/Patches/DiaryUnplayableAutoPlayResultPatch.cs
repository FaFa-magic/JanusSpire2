using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Singleton;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class DiaryUnplayableAutoPlayResultPatch : IPatchMethod
{
    public static string PatchId => "janus_diary_unplayable_auto_play_result";
    public static string Description => "Send cards that cannot be auto-played to the Diary when the effect requests it";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CardModel), nameof(CardModel.MoveToResultPileWithoutPlaying),
            [typeof(PlayerChoiceContext)])
    ];

    [HarmonyPrefix]
    public static bool Prefix(CardModel __instance, ref Task __result)
    {
        if (!JanusSingleton.HasDiaryPlayResult(__instance) ||
            __instance.Pile?.Type != PileType.Play ||
            __instance.IsDupe)
        {
            return true;
        }

        __result = AddToDiary(__instance);
        return false;
    }

    private static async Task AddToDiary(CardModel card)
    {
        await CardPileCmd.Add(card, MainFile.Diary);
    }
}

using MegaCrit.Sts2.Core.Nodes;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

public class JanusPatches : IPatchMethod
{
    public static string PatchId => "test_log_release_game";

    public static string Description => "Print IsReleaseGame";
    
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
        [new(typeof(NGame), nameof(NGame.IsReleaseGame))];
    
    public static void Postfix(ref bool __result)
    {
        MainFile.Logger.Info($"NGame.IsReleaseGame = {__result}");
    }
}
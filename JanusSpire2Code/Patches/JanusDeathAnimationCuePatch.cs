using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Characters.Visuals;

namespace JanusSpire2.JanusSpire2Code.Patches;

public sealed class JanusDeathAnimationCuePatch : IPatchMethod
{
    public static string PatchId => "janus_death_animation_cue";
    public static string Description => "Use Janus's dead animation for the default death cue";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(ModCreatureVisualPlayback), nameof(ModCreatureVisualPlayback.TryPlayCue),
            [typeof(NCreatureVisuals), typeof(CharacterModel), typeof(string),
                typeof(ReadOnlySpan<string>), typeof(bool)])
    ];

    [HarmonyPrefix]
    public static void Prefix(
        CharacterModel? character,
        ref string primaryCue,
        ReadOnlySpan<string> alternateCueNames)
    {
        // Spine may log a missing animation instead of throwing, preventing RitsuLib's
        // fallback aliases from reaching "dead". Leave explicit cue lists untouched.
        if (character is JanusCharacter && alternateCueNames.IsEmpty &&
            string.Equals(primaryCue, "die", StringComparison.OrdinalIgnoreCase))
        {
            primaryCue = "dead";
        }
    }
}

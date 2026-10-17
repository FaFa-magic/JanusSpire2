using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Characters;
using JanusSpire2.JanusSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

/// <summary>
/// Bind Layer1 after the official counter refresh and restore its animation
/// material, which RefreshLabel overwrites. The base artwork lives in the PNG.
/// No replacement of the label, energy commands, UI lifecycle, or network state.
/// With no UI no callback runs; a missing child leaves the normal counter intact.
/// This optional presentation patch can fail independently after a game update.
/// </summary>
public sealed class JanusLiquidEnergyCounterPatch : IPatchMethod
{
    public static string PatchId => "janus_liquid_energy_counter";
    public static string Description => "Show Janus energy as a settling liquid level";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(NEnergyCounter), "RefreshLabel", Type.EmptyTypes)
    ];

    [HarmonyPostfix]
    public static void Postfix(NEnergyCounter __instance, Player ____player)
    {
        if (____player?.Character is not JanusCharacter || ____player.PlayerCombatState is not { } state)
            return;

        __instance.GetNodeOrNull<JanusLiquidEnergyOrb>("Layers/Layer1")?.UpdateEnergy(state);
    }
}

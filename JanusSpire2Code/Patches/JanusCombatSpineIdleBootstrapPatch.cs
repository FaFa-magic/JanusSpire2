using Godot;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Characters;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

/// <summary>
/// Restarts Janus's idle animation after RitsuLib replaces the combat Spine skeleton.
/// </summary>
public sealed class JanusCombatSpineIdleBootstrapPatch : IPatchMethod
{
	public static string PatchId => "janus_combat_spine_idle_bootstrap";

	public static string Description =>
		"Restart Janus's combat idle animation after the selected Spine skeleton is ready";

	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
		[new(typeof(NCreature), nameof(NCreature._Ready))];

	[HarmonyPostfix]
	[HarmonyPriority(Priority.Last)]
	public static void Postfix(NCreature __instance)
	{
		if (__instance.Entity?.Player?.Character is not JanusCharacter ||
		    __instance.Visuals?.SpineBody is not { } spineBody)
		{
			return;
		}

		// NCreature creates its CreatureAnimator during _Ready. RitsuLib then replaces
		// the selected skeleton in a postfix, which rebuilds Spine's animation state
		// and clears the idle track the animator just created. Deferring this callback
		// puts it after every _Ready postfix; RunWhenSpineReady also covers the native
		// skeleton's asynchronous initialization window.
		Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(__instance) ||
			    !__instance.IsInsideTree() ||
			    __instance.Entity.IsDead ||
			    __instance.Visuals?.SpineBody != spineBody)
			{
				return;
			}

			__instance.RunWhenSpineReady(spineBody, _ =>
			{
				if (GodotObject.IsInstanceValid(__instance) &&
				    __instance.IsInsideTree() &&
				    !__instance.Entity.IsDead)
				{
					__instance.SetAnimationTrigger(CreatureAnimator.idleTrigger);
				}
			});
		}).CallDeferred();
	}
}

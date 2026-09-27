using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

/// <summary>Local presentation only; the official transition still owns its Task and cancellation.</summary>
public sealed class JanusDiaryTransitionPatch : IPatchMethod
{
	public static string PatchId => "janus_diary_transition_duration";
	public static string Description => "Give Janus's diary transition time to appear and turn two pages";
	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NTransition), nameof(NTransition.FadeOut),
			[typeof(float), typeof(string), typeof(CancellationToken?)])
	];

	[HarmonyPrefix]
	public static void Prefix(ref float time, string transitionPath)
	{
		// No character/lobby singleton lookup: the caller already selected the local
		// character's material (also for multiplayer and loaded runs).
		if (transitionPath is not ("res://materials/transitions/janus_spire2_character_janus_character_transition_mat.tres"
			or "res://JanusSpire2/materials/janus_transition_mat.tres"))
			return;

		// 0.8 s -> 2 s. Keep explicit zero/invalid durations untouched. No extra
		// await, blocking delay, network message, or mutable global transition state.
		// Official Instant mode, tween cancellation, and message buffering remain
		// authoritative; FadeIn and RoomFadeOut are deliberately not patched.
		if (time > 0f && float.IsFinite(time) && time <= float.MaxValue / 2.5f)
			time *= 2.5f;
	}
}

using Godot;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Audio;
using JanusSpire2.JanusSpire2Code.Characters;
using JanusSpire2.JanusSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Helpers;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

internal static class JanusSkinSelectPanelController
{
	private const string ScenePath = "res://JanusSpire2/scenes/ui/janus_skin_select_panel.tscn";
	private static JanusSkinSelectPanel? _panelInstance;
	private static NCharacterSelectScreen? _audioOwnerScreen;

	public static void OnCharacterSelected(NCharacterSelectScreen screen, CharacterModel character)
	{
		if (!ReferenceEquals(_audioOwnerScreen, screen))
		{
			_audioOwnerScreen = screen;
			// Screen scope is explicit in RitsuLib 0.6.2. Ignore stale exits from
			// an older screen so they cannot stop a newer screen's voice.
			screen.TreeExiting += () =>
			{
				if (!ReferenceEquals(_audioOwnerScreen, screen))
					return;
				JanusAudio.StopCharacterSelectVoice();
				_audioOwnerScreen = null;
			};
		}
		if (character is not JanusCharacter janusSkin)
		{
			if (GodotObject.IsInstanceValid(_panelInstance))
				_panelInstance.Visible = false;
			return;
		}

		var infoPanel = screen.GetNodeOrNull<Control>("%InfoPanel");
		if (infoPanel is null)
			return;

		if (!GodotObject.IsInstanceValid(_panelInstance) || _panelInstance.GetParent() != infoPanel)
		{
			if (GodotObject.IsInstanceValid(_panelInstance))
				_panelInstance.QueueFreeSafely();

			var scene = ResourceLoader.Load<PackedScene>(ScenePath);
			if (scene is null)
				return;

			_panelInstance = scene.Instantiate<JanusSkinSelectPanel>(PackedScene.GenEditState.Disabled);
			infoPanel.AddChildSafely(_panelInstance);
			_panelInstance.Position = new Vector2(400f, 0f);
		}

		_panelInstance.SetInteractable(true);
		_panelInstance.ShowAndSync(screen, janusSkin);
	}

	public static void SetInteractable(bool interactable)
	{
		if (GodotObject.IsInstanceValid(_panelInstance))
			_panelInstance.SetInteractable(interactable);
	}
}

public sealed class JanusSkinSelectPatch : IPatchMethod
{
	public static string PatchId => "janus_skin_select_panel";
	public static string Description => "Show the Janus Spine skin selector";
	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
	[
		new(
			typeof(NCharacterSelectScreen),
			nameof(NCharacterSelectScreen.SelectCharacter),
			[typeof(NCharacterSelectButton), typeof(CharacterModel)])
	];

	[HarmonyPostfix]
	public static void Postfix(NCharacterSelectScreen __instance, CharacterModel characterModel)
	{
		JanusSkinSelectPanelController.OnCharacterSelected(__instance, characterModel);
	}

	[HarmonyPrefix]
	public static void Prefix() => JanusAudio.StopCharacterSelectVoice();
}

public sealed class JanusCharacterSelectVoicePatch : IPatchMethod
{
	public static string PatchId => "janus_character_select_voice_lifecycle";
	public static string Description => "Keep Janus's character-select voice stoppable across selection and transition";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NAudioManager), nameof(NAudioManager.PlayOneShot),
			[typeof(string), typeof(Dictionary<string, float>), typeof(float)])
	];

	[HarmonyPrefix]
	[HarmonyPriority(Priority.First)]
	public static bool Prefix(string path, float volume)
	{
		if (path == JanusAudio.CharacterTransitionEvent)
			JanusAudio.StopCharacterSelectVoice();

		if (path != JanusAudio.CharacterSelectEvent || TestMode.IsOn)
			return true;

		JanusAudio.PlayCharacterSelectVoice(volume);
		return false;
	}
}

public sealed class JanusSkinSelectEmbarkPatch : IPatchMethod
{
	public static string PatchId => "janus_skin_select_panel_embark";
	public static string Description => "Stop the character-select voice on embark or when leaving the screen";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NCharacterSelectScreen), "OnEmbarkPressed", null),
		new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.OnSubmenuClosed), [])
	];

	[HarmonyPostfix]
	public static void Postfix() => JanusSkinSelectPanelController.SetInteractable(false);

	[HarmonyPrefix]
	public static void Prefix() => JanusAudio.StopCharacterSelectVoice();
}

public sealed class JanusSkinSelectUnreadyPatch : IPatchMethod
{
	public static string PatchId => "janus_skin_select_panel_unready";
	public static string Description => "Unlock the Janus skin selector after unreadying";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() => [new(typeof(NCharacterSelectScreen), "OnUnreadyPressed", null)];

	[HarmonyPostfix]
	public static void Postfix() => JanusSkinSelectPanelController.SetInteractable(true);
}

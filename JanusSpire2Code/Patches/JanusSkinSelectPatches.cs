using Godot;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Audio;
using JanusSpire2.JanusSpire2Code.Characters;
using JanusSpire2.JanusSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;
using MegaCrit.Sts2.Core.TestSupport;
using STS2RitsuLib.Patching.Models;
using System.Runtime.CompilerServices;

namespace JanusSpire2.JanusSpire2Code.Patches;

internal static class JanusSkinSelectPanelController
{
	private const string ScenePath = "res://JanusSpire2/scenes/ui/janus_skin_select_panel.tscn";
	private const string PanelName = "JanusSkinSelectPanel";
	private static readonly ConditionalWeakTable<Control, object> ObservedScreens = new();
	private static Control? _audioOwnerScreen;

	internal static void BeginSelection(Control screen)
	{
		JanusAudio.StopCharacterSelectVoice();
		_audioOwnerScreen = screen;
		ObservedScreens.GetValue(screen, static observed =>
		{
			observed.VisibilityChanged += () =>
			{
				if (!observed.IsVisibleInTree())
					StopVoice(observed);
			};
			observed.TreeExiting += () =>
			{
				if (ReferenceEquals(_audioOwnerScreen, observed))
				{
					StopVoice(observed);
					_audioOwnerScreen = null;
				}
			};
			return new object();
		});
	}

	internal static void StopVoice(Control screen)
	{
		if (ReferenceEquals(_audioOwnerScreen, screen))
			JanusAudio.StopCharacterSelectVoice();
	}

	internal static void Refresh(Control screen)
	{
		var lobby = ((ICharacterSelectButtonDelegate)screen).Lobby;
		Control? parent = GetPanelParent(screen);
		if (lobby is null || parent is null)
			return;

		var player = lobby.LocalPlayer;
		var panel = parent.GetNodeOrNull<JanusSkinSelectPanel>(PanelName);
		if (player.isRotatingDread || player.character is not JanusCharacter janusSkin)
		{
			panel?.Hide();
			return;
		}

		if (panel is null)
		{
			var scene = ResourceLoader.Load<PackedScene>(ScenePath);
			if (scene is null)
				return;

			panel = scene.Instantiate<JanusSkinSelectPanel>(PackedScene.GenEditState.Disabled);
			parent.AddChildSafely(panel);
			if (screen is NDreadScreen)
				panel.SetDreadLayout();
			else
				panel.Position = new Vector2(400f, 0f);
		}

		panel.SetInteractable(!player.isReady);
		panel.ShowAndSync(lobby, janusSkin);
	}

	internal static void Hide(Control screen) =>
		GetPanelParent(screen)?.GetNodeOrNull<JanusSkinSelectPanel>(PanelName)?.Hide();

	internal static void SetInteractable(Control screen, bool interactable) =>
		GetPanelParent(screen)?.GetNodeOrNull<JanusSkinSelectPanel>(PanelName)?.SetInteractable(interactable);

	private static Control? GetPanelParent(Control screen) =>
		screen is NDreadScreen ? screen : screen.GetNodeOrNull<Control>("InfoPanel");
}

public sealed class JanusSkinSelectPatch : IPatchMethod
{
	public static string PatchId => "janus_skin_select_panel";
	public static string Description => "Show the Janus Spine skin selector";
	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter), [typeof(NCharacterSelectButton)]),
		new(typeof(NBlitzScreen), nameof(NBlitzScreen.SelectCharacter), [typeof(NCharacterSelectButton)]),
		new(typeof(NDreadScreen), nameof(NDreadScreen.SelectCharacter), [typeof(NCharacterSelectButton)])
	];

	[HarmonyPostfix]
	public static void Postfix(Control __instance, NCharacterSelectButton charSelectButton)
	{
		if (!charSelectButton.IsRandom && !charSelectButton.IsLocked && charSelectButton.Character is JanusCharacter)
			JanusSkinSelectPanelController.Refresh(__instance);
		else
			JanusSkinSelectPanelController.Hide(__instance);
	}

	[HarmonyPrefix]
	public static void Prefix(Control __instance) => JanusSkinSelectPanelController.BeginSelection(__instance);
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
		new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.OnSubmenuClosed), []),
		new(typeof(NBlitzScreen), "OnEmbarkPressed", null),
		new(typeof(NBlitzScreen), nameof(NBlitzScreen.OnSubmenuClosed), []),
		new(typeof(NDreadScreen), "OnEmbarkPressed", null),
		new(typeof(NDreadScreen), nameof(NDreadScreen.OnSubmenuClosed), [])
	];

	[HarmonyPostfix]
	public static void Postfix(Control __instance) => JanusSkinSelectPanelController.SetInteractable(__instance, false);

	[HarmonyPrefix]
	public static void Prefix(Control __instance) => JanusSkinSelectPanelController.StopVoice(__instance);
}

public sealed class JanusSkinSelectUnreadyPatch : IPatchMethod
{
	public static string PatchId => "janus_skin_select_panel_unready";
	public static string Description => "Unlock the Janus skin selector after unreadying";
	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NCharacterSelectScreen), "OnUnreadyPressed", null),
		new(typeof(NBlitzScreen), "OnUnreadyPressed", null),
		new(typeof(NDreadScreen), "OnUnreadyPressed", null)
	];

	[HarmonyPostfix]
	public static void Postfix(Control __instance) => JanusSkinSelectPanelController.SetInteractable(__instance, true);
}

public sealed class JanusDreadRotationTogglePatch : IPatchMethod
{
	public static string PatchId => "janus_dread_skin_panel_rotation_toggle";
	public static string Description => "Refresh the Janus skin panel when the Dread rotation selection changes";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() =>
		[new(typeof(NDreadScreen), nameof(NDreadScreen.BeginRotationTickboxChanged), [typeof(bool)])];

	[HarmonyPostfix]
	public static void Postfix(NDreadScreen __instance) => JanusSkinSelectPanelController.Refresh(__instance);
}

public sealed class JanusSpecialModeRemoteSelectionPatch : IPatchMethod
{
	public static string PatchId => "janus_special_mode_remote_skin_selection";
	public static string Description => "Show remote Janus skin selections on the base Janus lobby button";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NBlitzScreen), "RefreshButtonSelectionForPlayer", [typeof(StartRunLobbyPlayer)]),
		new(typeof(NDreadScreen), "RefreshButtonSelectionForPlayer", [typeof(StartRunLobbyPlayer)])
	];

	[HarmonyPrefix]
	public static void Prefix(ref StartRunLobbyPlayer player)
	{
		// Only the button lookup uses the shared character. The lobby keeps the exact skin ID.
		if (player.character is JanusSkinVariant)
			player.character = ModelDb.Character<JanusCharacter>();
	}
}

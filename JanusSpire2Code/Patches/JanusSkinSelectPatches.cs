using Godot;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Audio;
using JanusSpire2.JanusSpire2Code.Characters;
using JanusSpire2.JanusSpire2Code.Configs;
using JanusSpire2.JanusSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;
using MegaCrit.Sts2.Core.Saves;
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
		if (player.character is not JanusCharacter janusSkin)
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
				panel.SetScreenLayout(new Vector2(0.75f, 0.48f));
			else if (screen is NCustomRunScreen)
				panel.SetScreenLayout(new Vector2(0.07f, 0.43f), scale: 0.36f);
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
		screen is NDreadScreen or NCustomRunScreen ? screen : screen.GetNodeOrNull<Control>("InfoPanel");
}

public sealed class JanusSkinSelectPatch : IPatchMethod
{
	[ThreadStatic]
	private static StartRunLobby? _selectionLobby;

	internal static StartRunLobby? SelectionLobby => _selectionLobby;

	public static string PatchId => "janus_skin_select_panel";
	public static string Description => "Show the Janus Spine skin selector";
	public static bool IsCritical => false;

	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter), [typeof(NCharacterSelectButton)]),
		new(typeof(NBlitzScreen), nameof(NBlitzScreen.SelectCharacter), [typeof(NCharacterSelectButton)]),
		new(typeof(NDreadScreen), nameof(NDreadScreen.SelectCharacter), [typeof(NCharacterSelectButton)]),
		new(typeof(NCustomRunScreen), nameof(NCustomRunScreen.SelectCharacter), [typeof(NCharacterSelectButton)])
	];

	[HarmonyPostfix]
	public static void Postfix(Control __instance, NCharacterSelectButton charSelectButton)
	{
		if (__instance is NDreadScreen)
			return;

		if (!charSelectButton.IsRandom && !charSelectButton.IsLocked && charSelectButton.Character is JanusCharacter)
			JanusSkinSelectPanelController.Refresh(__instance);
		else
			JanusSkinSelectPanelController.Hide(__instance);
	}

	[HarmonyPrefix]
	public static void Prefix(Control __instance, NCharacterSelectButton charSelectButton, out StartRunLobby? __state)
	{
		__state = _selectionLobby;
		JanusSkinSelectPanelController.BeginSelection(__instance);
		_selectionLobby = charSelectButton.IsLocked ? null : ((ICharacterSelectButtonDelegate)__instance).Lobby;
	}

	[HarmonyFinalizer]
	public static void Finalizer(StartRunLobby? __state) => _selectionLobby = __state;
}

public sealed class JanusRestorePreferredSkinPatch : IPatchMethod
{
	public static string PatchId => "janus_restore_preferred_skin";
	public static string Description => "Select the locally saved Janus outfit through the normal lobby character message";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() =>
		[new(typeof(StartRunLobby), nameof(StartRunLobby.SetLocalCharacter), [typeof(CharacterModel), typeof(bool)])];

	[HarmonyPrefix]
	public static void Prefix(StartRunLobby __instance, ref CharacterModel character)
	{
		if (ReferenceEquals(__instance, JanusSkinSelectPatch.SelectionLobby) &&
			character is JanusCharacter { CurrentSkin: JanusSkin.Default } janus)
		{
			character = JanusSkinSelectPanel.ResolvePreferredSkin(JanusConfigPage.LastPlayedSkinBinding.Read(), janus);
		}
	}
}

public sealed class JanusSavePreferredSkinPatch : IPatchMethod
{
	private static readonly Type[] BeginRunParameters =
		[typeof(string), typeof(List<ActModel>), typeof(IReadOnlyList<ModifierModel>)];

	public static string PatchId => "janus_save_preferred_skin";
	public static string Description => "Remember the local player's chosen Janus outfit when the lobby starts a run";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.BeginRun), BeginRunParameters),
		new(typeof(NBlitzScreen), nameof(NBlitzScreen.BeginRun), BeginRunParameters),
		new(typeof(NDreadScreen), nameof(NDreadScreen.BeginRun), BeginRunParameters),
		new(typeof(NCustomRunScreen), nameof(NCustomRunScreen.BeginRun), BeginRunParameters)
	];

	[HarmonyPrefix]
	public static void Prefix(ICharacterSelectButtonDelegate __instance, NCharacterSelectButton? ____selectedButton)
		=> RememberSkin(____selectedButton?.Character, __instance.Lobby.LocalPlayer.character);

	internal static void RememberSkin(CharacterModel? selected, CharacterModel? localCharacter)
	{
		if (selected is not (JanusCharacter or RotatingCharacter) ||
			localCharacter is not JanusCharacter janus ||
			JanusConfigPage.LastPlayedSkinBinding.Read() == janus.CurrentSkin)
			return;

		JanusConfigPage.LastPlayedSkinBinding.Write(janus.CurrentSkin);
		JanusConfigPage.LastPlayedSkinBinding.Save();
	}
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
		new(typeof(NDreadScreen), nameof(NDreadScreen.OnSubmenuClosed), []),
		new(typeof(NCustomRunScreen), "OnEmbarkPressed", null),
		new(typeof(NCustomRunScreen), nameof(NCustomRunScreen.OnSubmenuClosed), [])
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
		new(typeof(NDreadScreen), "OnUnreadyPressed", null),
		new(typeof(NCustomRunScreen), "OnUnreadyPressed", null)
	];

	[HarmonyPostfix]
	public static void Postfix(Control __instance) => JanusSkinSelectPanelController.SetInteractable(__instance, true);
}

public sealed class JanusDreadSkinPanelPatch : IPatchMethod
{
	public static string PatchId => "janus_dread_skin_panel";
	public static string Description => "Show the skin selector when the Dread selection resolves to Janus";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NDreadScreen), nameof(NDreadScreen.SelectCharacter), [typeof(NCharacterSelectButton)]),
		new(typeof(NDreadScreen), "SetSelectedCharacterVisuals", [typeof(CharacterModel)])
	];

	[HarmonyPostfix]
	public static void Postfix(
		NDreadScreen __instance,
		NCharacterSelectButton? ____selectedButton,
		CharacterModel? ____rotatingDisplayCharacterOverride,
		CharacterModel? ____rotatingCharacter)
	{
		if (____selectedButton is { IsLocked: false } button &&
			ResolveDisplayedCharacter(button.Character, ____rotatingDisplayCharacterOverride, ____rotatingCharacter)
				is JanusCharacter)
		{
			JanusSkinSelectPanelController.Refresh(__instance);
		}
		else
		{
			JanusSkinSelectPanelController.Hide(__instance);
		}
	}

	internal static CharacterModel? ResolveDisplayedCharacter(
		CharacterModel selected, CharacterModel? rotationOverride, CharacterModel? rotatingCharacter) =>
		selected is RotatingCharacter ? rotationOverride ?? rotatingCharacter : selected;
}

public sealed class JanusDreadRotationTogglePatch : IPatchMethod
{
	public static string PatchId => "janus_dread_skin_panel_rotation_toggle";
	public static string Description => "Keep the selected Janus outfit when the Dread rotation checkbox changes";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() =>
		[new(typeof(NDreadScreen), nameof(NDreadScreen.BeginRotationTickboxChanged), [typeof(bool)])];

	[HarmonyPrefix]
	public static void Prefix(NDreadScreen __instance, bool beginRotation, NCharacterSelectButton? ____selectedButton)
	{
		if (____selectedButton?.Character is not JanusCharacter)
			return;

		var lobby = __instance.Lobby;
		var player = lobby.LocalPlayer;
		// Update the official flag with the exact skin so the original skips its base-button selection.
		if (player.isRotatingDread != beginRotation && player.character is JanusSkinVariant skin)
		{
			lobby.SetLocalCharacter(skin, beginRotation);
		}
	}
}

public sealed class JanusDreadRotationIdPatch : IPatchMethod
{
	public static string PatchId => "janus_dread_rotation_character_id";
	public static string Description => "Resolve saved Janus outfits to the shared Dread rotation character";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() =>
		[new(typeof(AscensionHelper), nameof(AscensionHelper.GetRotatingCharacterIdForStreak), [typeof(ModelId), typeof(int)])];

	[HarmonyPrefix]
	public static void Prefix(ref ModelId startingCharacter) =>
		startingCharacter = JanusSharedProgression.GetProgressionId(startingCharacter);
}

public sealed class JanusDreadSaveRotationPatch : IPatchMethod
{
	public static string PatchId => "janus_dread_save_rotation_character";
	public static string Description => "Save the base Janus identity as the Dread rotation starting character";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() =>
		[new(typeof(NDreadScreen), "UpdateProgressForRotation", [typeof(CharacterModel)])];

	[HarmonyPrefix]
	public static void Prefix(ref CharacterModel embarkingCharacter)
	{
		if (embarkingCharacter is JanusSkinVariant)
			embarkingCharacter = ModelDb.Character<JanusCharacter>();
	}
}

public sealed class JanusDreadPreferredCharacterPatch : IPatchMethod
{
	public static string PatchId => "janus_dread_preferred_character";
	public static string Description => "Restore Dread focus to Janus when saved preferences refer to an outfit";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() =>
		[new(typeof(NDreadScreen), nameof(NDreadScreen.OnSubmenuOpened), [])];

	[HarmonyPrefix]
	public static void Prefix(NDreadScreen __instance)
	{
		JanusSkinSelectPanelController.Hide(__instance);
		var progress = SaveManager.Instance.Progress;
		if (__instance.Lobby.NetService.Type == NetGameType.Singleplayer)
			progress.PreferredDreadCharacter = JanusSharedProgression.GetProgressionId(progress.PreferredDreadCharacter);
		else
			progress.PreferredMultiplayerDreadCharacter = JanusSharedProgression.GetProgressionId(progress.PreferredMultiplayerDreadCharacter);
	}
}

public sealed class JanusSpecialModeRemoteSelectionPatch : IPatchMethod
{
	public static string PatchId => "janus_special_mode_remote_skin_selection";
	public static string Description => "Show remote Janus skin selections on the base Janus lobby button";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NBlitzScreen), "RefreshButtonSelectionForPlayer", [typeof(StartRunLobbyPlayer)]),
		new(typeof(NBlitzScreen), nameof(NBlitzScreen.RemotePlayerDisconnected), [typeof(StartRunLobbyPlayer)]),
		new(typeof(NCharacterSelectScreen), "RefreshButtonSelectionForPlayer", [typeof(StartRunLobbyPlayer)]),
		new(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.RemotePlayerDisconnected), [typeof(StartRunLobbyPlayer)]),
		new(typeof(NDreadScreen), "RefreshButtonSelectionForPlayer", [typeof(StartRunLobbyPlayer)]),
		new(typeof(NDreadScreen), nameof(NDreadScreen.RemotePlayerDisconnected), [typeof(StartRunLobbyPlayer)]),
		new(typeof(NCustomRunScreen), "RefreshButtonSelectionForPlayer", [typeof(StartRunLobbyPlayer)]),
		new(typeof(NCustomRunScreen), nameof(NCustomRunScreen.RemotePlayerDisconnected), [typeof(StartRunLobbyPlayer)])
	];

	[HarmonyPrefix]
	public static void Prefix(ref StartRunLobbyPlayer player)
	{
		// Only the button lookup uses the shared character. The lobby keeps the exact skin ID.
		if (player.character is JanusSkinVariant)
			player.character = ModelDb.Character<JanusCharacter>();
	}
}

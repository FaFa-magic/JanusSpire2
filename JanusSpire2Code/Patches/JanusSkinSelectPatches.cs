using Godot;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Characters;
using JanusSpire2.JanusSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

internal static class JanusSkinSelectPanelController
{
	private const string ScenePath = "res://JanusSpire2/scenes/ui/janus_skin_select_panel.tscn";
	private static JanusSkinSelectPanel? _panelInstance;

	public static void OnCharacterSelected(NCharacterSelectScreen screen, CharacterModel character)
	{
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
}

public sealed class JanusSkinSelectEmbarkPatch : IPatchMethod
{
	public static string PatchId => "janus_skin_select_panel_embark";
	public static string Description => "Lock the Janus skin selector while embarking";
	public static bool IsCritical => false;
	public static ModPatchTarget[] GetTargets() => [new(typeof(NCharacterSelectScreen), "OnEmbarkPressed", null)];

	[HarmonyPostfix]
	public static void Postfix() => JanusSkinSelectPanelController.SetInteractable(false);
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

using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using JanusSpire2.JanusSpire2Code.Characters;

namespace JanusSpire2.JanusSpire2Code.Nodes;

[GlobalClass]
public partial class JanusSkinSelectPanel : Control
{
	private static readonly JanusCharacter[] Skins =
	[
		ModelDb.Character<JanusCharacter>(),
		ModelDb.Character<JanusVariantThree>(),
		ModelDb.Character<JanusVariantFour>(),
		ModelDb.Character<JanusVariantFive>(),
		ModelDb.Character<JanusVariantSix>(),
		ModelDb.Character<JanusVariantSeven>(),
		ModelDb.Character<JanusVariantH>()
	];

	private TextureButton _leftArrow = null!;
	private TextureButton _rightArrow = null!;
	private Control _visualContainer = null!;
	private Label _skinNameLabel = null!;
	private NCharacterSelectScreen _selectScreen = null!;
	private Node2D? _currentVisualNode;
	private ModelId? _renderedSkinId;
	private int _currentIndex;

	public override void _Ready()
	{
		_leftArrow = GetNode<TextureButton>("VBoxContainer/HBoxContainer/LeftArrow");
		_rightArrow = GetNode<TextureButton>("VBoxContainer/HBoxContainer/RightArrow");
		_visualContainer = GetNode<Control>("VBoxContainer/HBoxContainer/VisualContainer");
		_skinNameLabel = GetNode<Label>("VBoxContainer/LabelContainer/SkinNameLabel");

		_leftArrow.Pressed += OnLeftPressed;
		_rightArrow.Pressed += OnRightPressed;
	}

	public void SetInteractable(bool interactable)
	{
		if (GodotObject.IsInstanceValid(_leftArrow))
		{
			_leftArrow.Visible = interactable;
			_leftArrow.Disabled = !interactable;
		}

		if (GodotObject.IsInstanceValid(_rightArrow))
		{
			_rightArrow.Visible = interactable;
			_rightArrow.Disabled = !interactable;
		}
	}

	public void ShowAndSync(NCharacterSelectScreen screen, JanusCharacter selectedSkin)
	{
		_selectScreen = screen;
		Visible = true;

		var selectedIndex = Array.FindIndex(Skins, skin => skin.Id == selectedSkin.Id);
		_currentIndex = selectedIndex >= 0 ? selectedIndex : 0;

		// SelectCharacter 已经同步过大厅；这里只刷新面板，避免重复发送联机消息。
		RenderSkinVisuals(Skins[_currentIndex]);
	}

	private void OnLeftPressed()
	{
		_currentIndex = (_currentIndex + Skins.Length - 1) % Skins.Length;
		ApplySelection();
	}

	private void OnRightPressed()
	{
		_currentIndex = (_currentIndex + 1) % Skins.Length;
		ApplySelection();
	}

	private void ApplySelection()
	{
		var skin = Skins[_currentIndex];
		_selectScreen.Lobby.SetLocalCharacter(skin);
		RenderSkinVisuals(skin);
	}

	private void RenderSkinVisuals(JanusCharacter skin)
	{
		_skinNameLabel.Text = new LocString("characters", $"{skin.Id.Entry}.skinName").GetFormattedText();

		if (_renderedSkinId == skin.Id && GodotObject.IsInstanceValid(_currentVisualNode))
			return;

		_renderedSkinId = skin.Id;
		if (GodotObject.IsInstanceValid(_currentVisualNode))
		{
			_visualContainer.RemoveChild(_currentVisualNode);
			_currentVisualNode.QueueFree();
			_currentVisualNode = null;
		}

		GD.Print($"[JanusSpire2] Skin preview loading: {skin.Id}");
		var scene = ResourceLoader.Load<PackedScene>(skin.CustomVisualsPath);
		if (scene is null)
			return;

		var visualNode = scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
		var spineNode = visualNode.GetNodeOrNull<Node2D>("%Visuals");
		if (!GodotObject.IsInstanceValid(spineNode) || spineNode.GetClass() != MegaSprite.spineClassName)
		{
			visualNode.Free();
			return;
		}

		var sprite = new MegaSprite((Variant)(GodotObject)spineNode);
		// The scene already contains the default skeleton. For variants, change it
		// before AddChild starts the SpineSprite's native initialization.
		if (skin.CurrentSkin != JanusSkin.Default)
		{
			var skeletonData = ResourceLoader.Load<Resource>(skin.CurrentSkinDefinition.SpineSkeletonDataPath);
			if (skeletonData is null)
			{
				visualNode.Free();
				return;
			}

			sprite.SetSkeletonDataRes(new MegaSkeletonDataResource(skeletonData));
		}

		_currentVisualNode = visualNode;
		_visualContainer.AddChild(visualNode);
		visualNode.Position = new Vector2(150f, 270f);
		visualNode.RunWhenSpineReady(sprite, animationState => animationState.SetAnimation("normal"));
		GD.Print($"[JanusSpire2] Skin preview attached: {skin.Id}");
	}
}

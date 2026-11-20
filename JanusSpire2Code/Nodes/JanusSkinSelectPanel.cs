using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
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
	private StartRunLobby _lobby = null!;
	private Node2D? _currentVisualNode;
	private ModelId? _renderedSkinId;
	private int _currentIndex;
	private bool _interactable = true;
	private NJanusIdleAnimation? _idleAnimation;

	internal static JanusCharacter ResolvePreferredSkin(JanusSkin preferred, JanusCharacter fallback) =>
		Array.Find(Skins, skin => skin.CurrentSkin == preferred) ?? fallback;

	public override void _Ready()
	{
		_leftArrow = GetNode<TextureButton>("VBoxContainer/HBoxContainer/LeftArrow");
		_rightArrow = GetNode<TextureButton>("VBoxContainer/HBoxContainer/RightArrow");
		_visualContainer = GetNode<Control>("VBoxContainer/HBoxContainer/VisualContainer");
		_skinNameLabel = GetNode<Label>("VBoxContainer/LabelContainer/SkinNameLabel");

		_leftArrow.Pressed += OnLeftPressed;
		_rightArrow.Pressed += OnRightPressed;
		VisibilityChanged += ResetIdleAnimation;
	}

	public void SetInteractable(bool interactable)
	{
		_interactable = interactable;
		ResetIdleAnimation();
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

	internal void SetScreenLayout(Vector2 anchor, float scale = 1f, Vector2 centerOffset = default)
	{
		var content = GetNode<Control>("VBoxContainer");
		content.AnchorLeft = content.AnchorRight = anchor.X;
		content.AnchorTop = content.AnchorBottom = anchor.Y;
		content.OffsetLeft = centerOffset.X - 350f * scale;
		content.OffsetTop = centerOffset.Y - 180f * scale;
		content.OffsetRight = content.OffsetLeft + 700f;
		content.OffsetBottom = content.OffsetTop + 360f;
		content.Scale = Vector2.One * scale;
	}

	internal void PlaceAboveSeed(Control seedInput)
	{
		var layout = GetNode<VBoxContainer>("VBoxContainer");
		var size = layout.Size;
		layout.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterTop, LayoutPresetMode.KeepSize);
		layout.GrowVertical = GrowDirection.Begin;
		layout.Position = new Vector2((Size.X - size.X) / 2f,
			seedInput.GlobalPosition.Y - GlobalPosition.Y - size.Y - 12f);
	}

	internal void ShowAndSync(StartRunLobby lobby, JanusCharacter selectedSkin)
	{
		_lobby = lobby;
		Visible = true;

		var selectedIndex = Array.FindIndex(Skins, skin => skin.Id == selectedSkin.Id);
		_currentIndex = selectedIndex >= 0 ? selectedIndex : 0;

		// SelectCharacter 已经同步过大厅；这里只刷新面板，避免重复发送联机消息。
		RenderSkinVisuals(Skins[_currentIndex]);
	}

	private void OnLeftPressed()
	{
		ApplySelection(-1);
	}

	private void OnRightPressed()
	{
		ApplySelection(1);
	}

	private void ApplySelection(int offset)
	{
		if (!_interactable || !IsVisibleInTree())
			return;

		var player = _lobby.LocalPlayer;
		if (player.isReady || player.character is not JanusCharacter)
			return;

		_currentIndex = (_currentIndex + Skins.Length + offset) % Skins.Length;
		var skin = Skins[_currentIndex];
		_lobby.SetLocalCharacter(skin, player.isRotatingDread);
		RenderSkinVisuals(skin);
	}

	private void RenderSkinVisuals(JanusCharacter skin)
	{
		ResetIdleAnimation();
		_skinNameLabel.Text = new LocString("characters", $"{skin.Id.Entry}.skinName").GetFormattedText();

		if (_renderedSkinId == skin.Id && GodotObject.IsInstanceValid(_currentVisualNode))
			return;

		_renderedSkinId = skin.Id;
		if (GodotObject.IsInstanceValid(_currentVisualNode))
		{
			_visualContainer.RemoveChild(_currentVisualNode);
			_currentVisualNode.QueueFree();
			_currentVisualNode = null;
			_idleAnimation = null;
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
		_idleAnimation = NJanusIdleAnimation.AttachPreview(visualNode, sprite, this, () => _interactable);
		GD.Print($"[JanusSpire2] Skin preview attached: {skin.Id}");
	}

	private void ResetIdleAnimation()
	{
		if (GodotObject.IsInstanceValid(_idleAnimation))
			_idleAnimation!.ResetActivity();
	}
}

using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Forms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.TestSupport;

namespace JanusSpire2.JanusSpire2Code.Nodes;

/// <summary>
/// Persistent Ianus Form presentation, hosted by the same creature FormVfx holder
/// used by Demon Form, Reaper Form, Void Form, and the other base-game forms.
/// </summary>
public partial class NIanvsFormVfx : NFormVfx
{
    private const int MaxFlowers = 25;
    private const int PositionAttempts = 12;
    private const float MinimumFlowerSpacing = 22f;
    private const float FlowerAreaMinX = -160f;
    private const float FlowerAreaMaxX = 160f;
    private const float FlowerAreaMinY = -28f;
    private const float FlowerAreaMaxY = 48f;
    private const float MinimumOpacity = 0.6f;
    private const float MaximumOpacity = 0.8f;
    private const string FlowerTexturePath =
        "res://JanusSpire2/images/vfx/ianvs_white_flower.png";

    private readonly Queue<Sprite2D> _flowers = new();
    private Texture2D? _flowerTexture;
    private int _pendingFlowers;

    public static NIanvsFormVfx? Create(Creature target)
    {
        if (TestMode.IsOn || NCombatRoom.Instance is not { } room)
        {
            return null;
        }

        var creatureNode = room.GetCreatureNode(target);
        if (target.Player is not { } player ||
            creatureNode is null ||
            creatureNode.Visuals.FormVfxHolder is null)
        {
            return null;
        }

        var vfx = new NIanvsFormVfx
        {
            Name = nameof(NIanvsFormVfx),
        };

        creatureNode.Visuals.AddFormVfx(vfx);
        vfx.Initialize(player);
        vfx.SetActive(true);
        return vfx;
    }

    public override void _Ready()
    {
        _flowerTexture = PreloadManager.Cache.GetTexture2D(FlowerTexturePath);

        int flowersToSpawn = _pendingFlowers;
        _pendingFlowers = 0;
        for (int i = 0; i < flowersToSpawn; i++)
        {
            SpawnFlower();
        }
    }

    public void GrowFlower()
    {
        if (!_isActive)
        {
            return;
        }

        if (!IsNodeReady())
        {
            _pendingFlowers = Math.Min(MaxFlowers, _pendingFlowers + 1);
            return;
        }

        SpawnFlower();
    }

    public override void SetActive(bool isActive)
    {
        base.SetActive(isActive);

        if (isActive)
        {
            Modulate = Colors.White;
            return;
        }

        if (IsNodeReady())
        {
            CreateTween()
                .TweenProperty(this, "modulate:a", 0f, 0.25)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Quad);
        }
    }

    private void SpawnFlower()
    {
        if (_flowerTexture is null)
        {
            return;
        }

        while (_flowers.Count >= MaxFlowers)
        {
            FadeAndFree(_flowers.Dequeue());
        }

        Vector2 position = FindFlowerPosition();
        float depth = Mathf.InverseLerp(FlowerAreaMinY, FlowerAreaMaxY, position.Y);
        float targetScale = Rng.Chaotic.NextFloat(0.10f, 0.14f) + depth * 0.025f;
        float targetOpacity = Rng.Chaotic.NextFloat(MinimumOpacity, MaximumOpacity);

        var flower = new Sprite2D
        {
            Name = "WhiteFlower",
            Texture = _flowerTexture,
            Position = position,
            RotationDegrees = Rng.Chaotic.NextFloat(-16f, 16f),
            FlipH = Rng.Chaotic.NextBool(),
            Scale = Vector2.One * 0.01f,
            Modulate = new Color(1f, 1f, 1f, 0f),
        };

        AddChild(flower);
        _flowers.Enqueue(flower);

        Tween tween = CreateTween().SetParallel();
        tween.TweenProperty(flower, "scale", Vector2.One * targetScale, 0.3)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(flower, "modulate:a", targetOpacity, 0.18)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
    }

    private Vector2 FindFlowerPosition()
    {
        Vector2 candidate = Vector2.Zero;
        float minimumDistanceSquared = MinimumFlowerSpacing * MinimumFlowerSpacing;

        for (int attempt = 0; attempt < PositionAttempts; attempt++)
        {
            candidate = new Vector2(
                Rng.Chaotic.NextFloat(FlowerAreaMinX, FlowerAreaMaxX),
                Rng.Chaotic.NextFloat(FlowerAreaMinY, FlowerAreaMaxY));

            bool overlaps = false;
            foreach (Sprite2D flower in _flowers)
            {
                if (GodotObject.IsInstanceValid(flower) &&
                    flower.Position.DistanceSquaredTo(candidate) < minimumDistanceSquared)
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                return candidate;
            }
        }

        return candidate;
    }

    private void FadeAndFree(Sprite2D flower)
    {
        if (!GodotObject.IsInstanceValid(flower))
        {
            return;
        }

        Tween tween = CreateTween().SetParallel();
        tween.TweenProperty(flower, "scale", Vector2.Zero, 0.18)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(flower, "modulate:a", 0f, 0.14)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Quad);
        tween.Chain().TweenCallback(Callable.From(flower.QueueFree));
    }
}

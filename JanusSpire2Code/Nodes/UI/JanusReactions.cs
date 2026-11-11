using System.Runtime.CompilerServices;
using Godot;
using JanusSpire2.JanusSpire2Code.Characters;
using JanusSpire2.JanusSpire2Code.Configs;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Reaction;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace JanusSpire2.JanusSpire2Code.Nodes.UI;

internal static class JanusReactions
{
    private const string VanillaRoot = "res://images/ui/emote/";
    private const string JanusRoot = "res://JanusSpire2/images/ui/reactions/";
    private const float MaximumExtent = 140f;
    // The icon keeps its popup size on screen. Giving it more space in wheel units shrinks the ring.
    private const float WheelIconExtent = 130f;
    private static readonly string[] SlotFiles =
    [
        "exclaim.png", "skull.png", "thumb_down.png", "slime_sad.png",
        "question.png", "heart.png", "thumb_up.png", "happy_cultist.png"
    ];
    private static readonly ConditionalWeakTable<TextureRect, Artwork> ArtworkByControl = new();
    private static readonly ConditionalWeakTable<NReactionWheel, WheelStyle> WheelStyles = new();

    internal static IEnumerable<string> AssetPaths => SlotFiles.Select(file => JanusRoot + file);

    internal static bool ShouldShowJanusArt(ulong? senderId)
    {
        if (senderId is not { } id || !CombatManager.Instance.IsInProgress ||
            NCombatRoom.Instance is not { Mode: CombatRoomMode.ActiveCombat } room)
            return false;

        // Resolve the sending player, not the local viewer. The run roster also
        // works for a teammate whose combat visual has already been removed.
        var player = room.CreatureNodes.Select(node => node.Entity.Player).FirstOrDefault(p => p != null);
        return player?.RunState.GetPlayer(id)?.Character is JanusCharacter &&
            JanusConfigPage.CustomMultiplayerReactionsBinding.Read();
    }

    internal static int NextPopupIndex(NReactionContainer container, ulong? senderId)
        => ShouldShowJanusArt(senderId) ? container.GetChildCount() : -1;

    internal static void StyleAddedPopup(NReactionContainer container, int index)
    {
        // The vanilla dispatch appends the reaction synchronously. Use its
        // exact insertion index rather than styling an existing/last popup.
        // BeginAnim is short enough to be inlined in optimized game builds.
        if (index < 0 || index >= container.GetChildCount() ||
            container.GetChild(index) is not NReaction { } popup ||
            !popup.IsInsideTree() || GetArt(popup.Texture) is not { } texture || popup.Size.X <= 0f)
            return;

        float scale = DisplayExtent(popup.GetViewportRect().Size) / popup.Size.X;
        if (scale <= 0f)
            return;

        // Vanilla has already centered the popup at the cursor. Scale about its
        // center and keep the native tween, lifetime, and Texture/Type intact.
        SetArt(popup, texture, popup.Size.X);
        popup.PivotOffset = popup.Size * 0.5f;
        popup.Scale = Vector2.One * scale;
    }

    internal static void StyleWheel(NReactionWheel wheel, bool showJanus)
    {
        var style = WheelStyles.GetValue(wheel, node => new WheelStyle(node.Scale));
        float extent = DisplayExtent(wheel.GetViewportRect().Size);
        var wedges = wheel.GetChildren().OfType<NReactionWheelWedge>().ToArray();
        var overlays = new List<TextureRect>(wedges.Length);
        foreach (var wedge in wedges)
        {
            if (wedge.GetNodeOrNull<TextureRect>("TextureRect") is not { } icon)
                continue;
            var texture = showJanus ? GetArt(icon.Texture) : null;
            // The native slot has its own offset and counter-rotation inside the wedge.
            Vector2 center = icon.GetTransform().AffineInverse() * (wedge.Size * 0.5f);
            if (SetArt(icon, texture, WheelIconExtent, center) is { } overlay)
                overlays.Add(overlay);
        }

        // The vanilla _Input already placed the wheel using its prior scale.
        // Keep the same center when entering/leaving Janus styling.
        Vector2 previousScale = wheel.Scale;
        wheel.Scale = overlays.Count > 0 ? Vector2.One * (extent / WheelIconExtent) : style.OriginalScale;
        wheel.GlobalPosition += (previousScale - wheel.Scale) * wheel.Size * 0.5f;
        if (overlays.Count > 0)
            KeepWheelOnScreen(wheel, wedges, overlays);
    }

    internal static float DisplayExtent(Vector2 viewport)
    {
        float shortSide = Mathf.Min(viewport.X, viewport.Y);
        return shortSide > 0f ? Mathf.Min(MaximumExtent, shortSide * 0.14f) : MaximumExtent;
    }

    private static Texture2D? GetArt(Texture2D? vanilla)
    {
        if (vanilla?.ResourcePath is not { } path ||
            !path.StartsWith(VanillaRoot, StringComparison.Ordinal))
            return null;

        string file = path[VanillaRoot.Length..];
        if (!SlotFiles.Contains(file))
            return null;
        string modPath = JanusRoot + file;
        return ResourceLoader.Exists(modPath) ? ResourceLoader.Load<Texture2D>(modPath) : null;
    }

    private static TextureRect? SetArt(TextureRect target, Texture2D? texture, float extent, Vector2? center = null)
    {
        if (texture == null)
        {
            if (ArtworkByControl.TryGetValue(target, out var existing))
            {
                existing.Overlay.Hide();
                target.SelfModulate = existing.OriginalTint;
            }
            return null;
        }

        var artwork = ArtworkByControl.GetValue(target, control =>
        {
            var overlay = new TextureRect
            {
                Name = "JanusReactionArt",
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Linear,
                SelfModulate = control.SelfModulate
            };
            control.AddChild(overlay);
            overlay.SetAnchorsPreset(Control.LayoutPreset.Center);
            return new Artwork(overlay, control.SelfModulate);
        });

        artwork.Overlay.Texture = texture;
        artwork.Overlay.Size = Vector2.One * extent;
        artwork.Overlay.Position = (center ?? target.Size * 0.5f) - artwork.Overlay.Size * 0.5f;
        artwork.Overlay.Show();
        var tint = artwork.OriginalTint;
        tint.A = 0f; // SelfModulate does not tint the child artwork.
        target.SelfModulate = tint;
        return artwork.Overlay;
    }

    private static void KeepWheelOnScreen(
        NReactionWheel wheel, NReactionWheelWedge[] wedges, List<TextureRect> overlays)
    {
        Rect2 bounds = GlobalBounds(wedges[0]);
        foreach (var wedge in wedges.Skip(1))
            bounds = bounds.Merge(GlobalBounds(wedge));
        foreach (var overlay in overlays)
            bounds = bounds.Merge(GlobalBounds(overlay));
        // Allow the official hover tween to push the wedge another 25 units outward.
        bounds = bounds.Grow(25f * wheel.Scale.X + 8f);
        Rect2 viewport = wheel.GetViewportRect();
        wheel.GlobalPosition += new Vector2(
            FitOffset(bounds.Position.X, bounds.End.X, viewport.Position.X, viewport.End.X),
            FitOffset(bounds.Position.Y, bounds.End.Y, viewport.Position.Y, viewport.End.Y));
    }

    private static Rect2 GlobalBounds(Control control)
    {
        Transform2D transform = control.GetGlobalTransform();
        var bounds = new Rect2(transform.Origin, Vector2.Zero);
        return bounds.Expand(transform * new Vector2(control.Size.X, 0))
            .Expand(transform * new Vector2(0, control.Size.Y))
            .Expand(transform * control.Size);
    }

    private static float FitOffset(float start, float end, float screenStart, float screenEnd)
    {
        if (end - start > screenEnd - screenStart)
            return (screenStart + screenEnd - start - end) * 0.5f;
        return Mathf.Clamp(0f, screenStart - start, screenEnd - end);
    }

    private sealed record Artwork(TextureRect Overlay, Color OriginalTint);
    private sealed record WheelStyle(Vector2 OriginalScale);
}

using Godot;
using JanusSpire2.JanusSpire2Code.Characters;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace JanusSpire2.JanusSpire2Code.Nodes;

/// <summary>Scene-owned cosmetic idle, shared by combat, skin preview and merchant actors.</summary>
public partial class NJanusIdleAnimation : Node
{
    internal const string TriggerPrefix = "JanusIdle_";
    internal static readonly string[] AnimationNames = ["stand", "stand2"];
    private const string ControllerName = "JanusIdleAnimation";
    private const double PollSeconds = 0.2d;
    private readonly JanusIdleClock _clock = new();
    private NCreature? _creature;
    private CanvasItem? _ambient;
    private Func<bool>? _interactable;
    private bool _resetOnInput;
    private MegaSprite _spine = null!;
    private MegaAnimationState? _state;
    private string[] _available = [];
    private string? _ownedAnimation;
    private int _nextAnimation;
    private double _pollElapsed;

    public static void AttachCombat(NCreature creature)
    {
        if (TestMode.IsOn || creature.Entity.Player?.Character is not JanusCharacter
            || creature.Visuals.SpineBody is not { } spine
            || creature.GetNodeOrNull<Node>(ControllerName) != null)
            return;
        creature.AddChildSafely(new NJanusIdleAnimation
        {
            Name = ControllerName, _creature = creature, _spine = spine
        });
    }

    public static NJanusIdleAnimation AttachPreview(Node2D visual, MegaSprite spine,
        Control panel, Func<bool> interactable)
        => AttachAmbient(visual, spine, panel, interactable, false);

    public static NJanusIdleAnimation AttachMerchant(Node2D visual, MegaSprite spine)
        => AttachAmbient(visual, spine, visual, () =>
        {
            var room = NMerchantRoom.Instance;
            return GodotObject.IsInstanceValid(room) && room!.IsAncestorOf(visual)
                && (ActiveScreenContext.Instance.IsCurrent(room)
                    || ActiveScreenContext.Instance.IsCurrent(room.Inventory));
        }, true);

    private static NJanusIdleAnimation AttachAmbient(Node2D visual, MegaSprite spine,
        CanvasItem visibility, Func<bool> interactable, bool resetOnInput)
    {
        if (visual.GetNodeOrNull<NJanusIdleAnimation>(ControllerName) is { } existing)
            return existing;
        var controller = new NJanusIdleAnimation
        {
            Name = ControllerName, _spine = spine, _ambient = visibility,
            _interactable = interactable, _resetOnInput = resetOnInput
        };
        visual.AddChildSafely(controller);
        return controller;
    }

    public static void NotifyActivity(Player player)
    {
        if (TestMode.IsOn || player.Character is not JanusCharacter)
            return;
        NCombatRoom.Instance?.GetCreatureNode(player.Creature)
            ?.GetNodeOrNull<NJanusIdleAnimation>(ControllerName)?.ResetActivity();
    }

    public override void _Ready()
    {
        SetProcessInput(_resetOnInput);
        this.RunWhenSpineReady(_spine, state =>
        {
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion())
                return;
            _state = state;
            _available = _spine.HasAnimation("normal")
                ? AnimationNames.Where(_spine.HasAnimation).ToArray() : [];
            if (_ambient != null && _spine.HasAnimation("normal"))
                state.SetAnimation("normal");
            if (_available.Length == 0)
            {
                SetProcess(false);
                SetProcessInput(false);
            }
        });
    }

    public void ResetActivity()
    {
        _clock.Reset();
        _pollElapsed = 0d;
        // Cancel only our own pose, not an attack/hit/death or another component's animation.
        if (_ownedAnimation != null && CurrentAnimation() == _ownedAnimation)
        {
            if (_creature != null)
                _creature.SetAnimationTrigger(CreatureAnimator.idleTrigger);
            else
                _state?.SetAnimation("normal");
        }
        _ownedAnimation = null;
    }

    public override void _Input(InputEvent @event)
    {
        // Shop clicks/keys (including purchases) restart the local visual timer;
        // don't consume input, reset remote combat actors, or count mouse motion.
        if (_resetOnInput && @event.IsPressed() && !@event.IsEcho() && CanIdle())
            ResetActivity();
    }

    public override void _Process(double delta)
    {
        if (_available.Length == 0)
            return;
        // Five native Spine queries per second; background stalls don't accrue a full timeout.
        _pollElapsed += Math.Clamp(delta, 0d, PollSeconds);
        if (_pollElapsed < PollSeconds)
            return;
        double elapsed = _pollElapsed;
        _pollElapsed = 0d;
        if (!EnsureAnimationState())
        {
            _clock.Reset();
            _ownedAnimation = null;
            return;
        }
        if (!CanIdle())
        {
            ResetActivity();
            return;
        }
        if (!_clock.Advance(elapsed, CurrentAnimation() == "normal"))
            return;

        _ownedAnimation = _available[_nextAnimation];
        _nextAnimation = (_nextAnimation + 1) % _available.Length;
        if (_creature != null)
            _creature.SetAnimationTrigger(TriggerPrefix + _ownedAnimation);
        else
        {
            _state!.SetAnimation(_ownedAnimation, loop: false);
            _state.AddAnimation("normal", loop: true);
        }
    }

    private bool CanIdle()
    {
        if (_creature == null)
            return GodotObject.IsInstanceValid(_ambient) && _ambient!.IsVisibleInTree()
                && (_interactable?.Invoke() ?? false);

        CombatManager combat = CombatManager.Instance;
        Player? player = _creature.Entity.Player;
        return _creature.IsVisibleInTree() && _creature.Entity.IsAlive
            && NCombatRoom.Instance?.GetCreatureNode(_creature.Entity) == _creature
            && combat.IsInProgress && !combat.IsEnding && !combat.IsPaused
            && player?.PlayerCombatState?.Phase == PlayerTurnPhase.Play
            && !combat.IsPlayerReadyToEndTurn(player)
            && RunManager.Instance.ActionQueueSet.IsEmpty
            && !RunManager.Instance.ActionExecutor.IsPaused;
    }

    private string? CurrentAnimation()
    {
        if (!EnsureAnimationState())
            return null;
        using var track = _state!.GetCurrent(0);
        return track?.GetAnimationName();
    }

    private bool EnsureAnimationState()
    {
        if (!GodotObject.IsInstanceValid(_spine.BoundObject))
        {
            _state = null;
            return false;
        }
        // The sprite owns this shared native state. Never Dispose it; reacquire
        // invalidated wrappers after a skeleton reload instead of using stale bindings.
        if (_state == null || !GodotObject.IsInstanceValid(_state.BoundObject))
            _state = _spine.TryGetAnimationState();
        return _state != null && GodotObject.IsInstanceValid(_state.BoundObject);
    }
}

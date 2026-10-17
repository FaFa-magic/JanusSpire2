using Godot;
using MegaCrit.Sts2.Core.Entities.Players;

namespace JanusSpire2.JanusSpire2Code.Nodes;

/// <summary>Presentation owned by one energy counter, never by the global/local player singleton.</summary>
public partial class JanusLiquidEnergyOrb : TextureRect
{
    private static readonly StringName FillRatio = "fill_ratio";
    private static readonly StringName MotionTime = "motion_time";
    private static readonly StringName MotionStrength = "motion_strength";
    private const float SettleTime = 1.65f;

    private PlayerCombatState? _state;
    private ShaderMaterial? _liquidMaterial;
    private bool _subscribed;
    private bool _initialized;
    private int _lastEnergy;
    private int _lastMaxEnergy;
    private float _displayedFill = 1f;
    private float _targetFill = 1f;
    private float _motionAge = SettleTime;
    private float _motionStrength;

    public override void _Ready()
    {
        // Each counter needs independent uniforms, including in multiplayer.
        if (Material is ShaderMaterial material)
        {
            _liquidMaterial = (ShaderMaterial)material.Duplicate();
            Material = _liquidMaterial;
        }
        UseParentMaterial = false;
        SetProcess(false);
        Subscribe();
        RefreshFromState();
    }

    public override void _EnterTree()
    {
        Subscribe();
        if (_liquidMaterial is not null)
        {
            _initialized = false;
            RefreshFromState();
        }
    }

    public override void _ExitTree() => Unsubscribe();

    public void UpdateEnergy(PlayerCombatState state)
    {
        // The artwork is the Layer1 PNG itself. The official label refresh
        // clears Layer1.Material; restore only this instance's animation material.
        if (_liquidMaterial is not null)
            Material = _liquidMaterial;

        if (!ReferenceEquals(_state, state))
        {
            Unsubscribe();
            _state = state;
            _initialized = false;
            Subscribe();
        }
        // Called after the official label refresh as well, so changes to maximum
        // energy are reflected even when current energy did not change.
        RefreshFromState();
    }

    private void Subscribe()
    {
        if (_state is null || _subscribed || !IsInsideTree())
            return;
        _state.EnergyChanged += OnEnergyChanged;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (_state is not null && _subscribed)
            _state.EnergyChanged -= OnEnergyChanged;
        _subscribed = false;
    }

    private void OnEnergyChanged(int oldEnergy, int newEnergy) => RefreshFromState();

    private void RefreshFromState()
    {
        if (_state is null || _liquidMaterial is null)
            return;

        int energy = _state.Energy;
        int maxEnergy = _state.MaxEnergy;
        // Extra energy fills the orb without changing the official numeric label.
        // A zero maximum still has a defined visual result and never divides by zero.
        float fill = maxEnergy > 0
            ? Mathf.Clamp((float)energy / maxEnergy, 0f, 1f)
            : energy > 0 ? 1f : 0f;

        if (!_initialized)
        {
            _initialized = true;
            _lastEnergy = energy;
            _lastMaxEnergy = maxEnergy;
            _targetFill = _displayedFill = fill;
            _motionAge = SettleTime;
            _motionStrength = 0f;
            PushUniforms();
            return;
        }

        if (energy == _lastEnergy && maxEnergy == _lastMaxEnergy)
            return;

        _lastEnergy = energy;
        _lastMaxEnergy = maxEnergy;
        _motionStrength = Mathf.Clamp(0.45f + Mathf.Abs(fill - _displayedFill) * 1.2f, 0.45f, 1f);
        _targetFill = fill;
        _motionAge = 0f;
        SetProcess(true);
        PushUniforms();
    }

    public override void _Process(double delta)
    {
        float elapsed = (float)delta;
        _motionAge = Mathf.Min(_motionAge + elapsed, SettleTime);
        _displayedFill = Mathf.Lerp(_displayedFill, _targetFill, 1f - Mathf.Exp(-12f * elapsed));
        if (_motionAge >= SettleTime)
        {
            _displayedFill = _targetFill;
            _motionStrength = 0f;
            SetProcess(false);
        }
        PushUniforms();
    }

    private void PushUniforms()
    {
        if (_liquidMaterial is null)
            return;
        _liquidMaterial.SetShaderParameter(FillRatio, _displayedFill);
        _liquidMaterial.SetShaderParameter(MotionTime, _motionAge);
        _liquidMaterial.SetShaderParameter(MotionStrength, _motionStrength);
    }
}

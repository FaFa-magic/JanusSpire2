using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Audio;
using JanusSpire2.JanusSpire2Code.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

/// <summary>
/// Establishes the player context for the synchronous attack-sound portion of
/// <see cref="CreatureCmd.TriggerAnim"/>. The animation itself is left untouched.
/// </summary>
public sealed class JanusAttackAudioContextPatch : IPatchMethod
{
    public static string PatchId => "janus_attack_audio_context";

    public static string Description => "Identify the Janus player whose attack animation is playing";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(CreatureCmd),
                nameof(CreatureCmd.TriggerAnim),
                [typeof(Creature), typeof(string), typeof(float)])
        ];
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    public static void Prefix(Creature creature, string triggerName, out bool __state)
    {
        __state = JanusAttackAudioTurnGate.TryEnter(creature, triggerName);
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(bool __state)
    {
        if (__state)
        {
            JanusAttackAudioTurnGate.Exit();
        }
    }
}

/// <summary>
/// Suppresses repeat plays of Janus's attack event during the same player turn.
/// This is presentation-only state and therefore never participates in combat
/// serialization or multiplayer checksums.
/// </summary>
public sealed class JanusAttackAudioPlaybackPatch : IPatchMethod
{
    public static string PatchId => "janus_attack_audio_once_per_turn";

    public static string Description => "Play Janus's attack voice only for her first attack each turn";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(SfxCmd),
                nameof(SfxCmd.Play),
                [typeof(string), typeof(float)])
        ];
    }

    [HarmonyPrefix]
    public static bool Prefix(string sfx)
    {
        return !string.Equals(sfx, JanusAudio.AttackEvent, StringComparison.Ordinal) ||
               JanusAttackAudioTurnGate.ConsumeCurrentTurnPlay();
    }
}

internal static class JanusAttackAudioTurnGate
{
    [ThreadStatic]
    private static Stack<Player>? _attackPlayers;

    private static readonly ConditionalWeakTable<Player, PlayerTurnState> PlayerStates = new();

    public static bool TryEnter(Creature creature, string triggerName)
    {
        if (!string.Equals(triggerName, "Attack", StringComparison.Ordinal) ||
            !creature.IsPlayer ||
            creature.Player is not { Character: JanusCharacter } player)
        {
            return false;
        }

        (_attackPlayers ??= new Stack<Player>()).Push(player);
        return true;
    }

    public static void Exit()
    {
        if (_attackPlayers is not { Count: > 0 })
        {
            return;
        }

        _attackPlayers.Pop();
        if (_attackPlayers.Count == 0)
        {
            _attackPlayers = null;
        }
    }

    public static bool ConsumeCurrentTurnPlay()
    {
        if (_attackPlayers is not { Count: > 0 })
        {
            // Preserve explicit/manual uses of AttackEvent that are not emitted by
            // a Janus attack animation.
            return true;
        }

        Player player = _attackPlayers.Peek();
        PlayerCombatState? combatState = player.PlayerCombatState;
        if (combatState == null)
        {
            return true;
        }

        PlayerTurnState state = PlayerStates.GetValue(player, static _ => new PlayerTurnState());
        if (!ReferenceEquals(state.CombatState, combatState) ||
            state.TurnNumber != combatState.TurnNumber)
        {
            state.CombatState = combatState;
            state.TurnNumber = combatState.TurnNumber;
            state.HasPlayed = false;
        }

        if (state.HasPlayed)
        {
            return false;
        }

        state.HasPlayed = true;
        return true;
    }

    private sealed class PlayerTurnState
    {
        public PlayerCombatState? CombatState { get; set; }

        public int TurnNumber { get; set; }

        public bool HasPlayed { get; set; }
    }
}

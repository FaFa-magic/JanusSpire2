using STS2RitsuLib.Audio;

namespace JanusSpire2.JanusSpire2Code.Audio;

public static class JanusAudio
{
	private const string CharacterSelectVoiceChannel = "janus_character_select_voice";
	private const string BankResource = "res://JanusSpire2/sfx/Janus.bank";
	private const string GuidResource = "res://JanusSpire2/sfx/Janus.guids.txt";
	private static IAudioHandle? _characterSelectVoice;
	private static bool _registered;
	private static bool _warnedPlaybackFailure;
	private static bool _warnedReleaseFailure;

	public const string AttackEvent = "event:/sfx/janus/attack";
	public const string CastEvent = "event:/sfx/janus/cast";
	public const string DeathEvent = "event:/sfx/janus/death";
	public const string CharacterSelectEvent = "event:/sfx/janus/character_select";
	public const string CharacterTransitionEvent = "event:/sfx/janus/character_transition";

	public static void PlayCharacterSelectVoice(float volume)
	{
		StopCharacterSelectVoice();
		// Do not lose a handle whose native release needs another cleanup attempt.
		if (_characterSelectVoice is not null)
			return;

		var playback = GameFmod.Playback.PlayOneShot(
			AudioSource.Event(CharacterSelectEvent),
			new AudioPlaybackOptions
			{
				Volume = volume,
				UseVanillaRouting = false,
				Scope = AudioLifecycleScope.Screen,
				AllowFadeOutOnStop = false,
				Routing = new AudioRoutingOptions
				{
					Channel = CharacterSelectVoiceChannel,
					ChannelMode = AudioChannelMode.ReplaceExisting,
					AllowFadeOutOnReplace = false
				}
			});
		_characterSelectVoice = playback.Handle;
		if ((!playback.Succeeded || _characterSelectVoice is null) && !_warnedPlaybackFailure)
		{
			_warnedPlaybackFailure = true;
			Godot.GD.PushWarning($"[JanusSpire2] Character-select voice failed: {playback.Status} {playback.Message}");
		}
	}

	public static void StopCharacterSelectVoice()
	{
		if (_characterSelectVoice is null)
			return;
		_characterSelectVoice.Dispose();
		if (_characterSelectVoice.IsReleased)
		{
			_characterSelectVoice = null;
			_warnedReleaseFailure = false;
		}
		else if (!_warnedReleaseFailure)
		{
			_warnedReleaseFailure = true;
			Godot.GD.PushWarning("[JanusSpire2] Character-select voice release failed; retaining the handle for cleanup retry.");
		}
	}

	public static void Register()
	{
		if (_registered)
			return;
		FmodStudioDeferredBankRegistration.RegisterBank(BankResource);
		FmodStudioDeferredBankRegistration.RegisterStudioGuidMappings(GuidResource);
		_registered = true;
	}
}

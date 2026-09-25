using STS2RitsuLib.Audio;

namespace JanusSpire2.JanusSpire2Code.Audio;

public static class JanusAudio
{
	private const string CharacterSelectVoiceChannel = "janus_character_select_voice";
	private const string CharacterSelectVoiceResource = "res://JanusSpire2/sfx/Janus_character_select.mp3";
	private static IAudioHandle? _characterSelectVoice;

	public const string AttackEvent = "event:/JanusSpire2/sfx/attack";
	public const string CastEvent = "event:/JanusSpire2/sfx/cast";
	public const string DeathEvent = "event:/JanusSpire2/sfx/death";
	public const string CharacterSelectEvent = "event:/JanusSpire2/sfx/character_select";
	public const string CharacterTransitionEvent = "event:/JanusSpire2/sfx/character_transition";

	public static void PlayCharacterSelectVoice(float volume)
	{
		StopCharacterSelectVoice();

		if (FmodStudioServer.TryCheckBusPath(FmodStudioRouting.SfxBus) == true)
			volume *= Math.Max(0f, FmodStudioBusAccess.TryGetVolume(FmodStudioRouting.SfxBus));

		var playback = GameFmod.Playback.PlayOneShot(
			AudioSource.ResourceFile(CharacterSelectVoiceResource),
			new AudioPlaybackOptions
			{
				Volume = volume,
				Scope = AudioLifecycleScope.Screen,
				Routing = new AudioRoutingOptions
				{
					Channel = CharacterSelectVoiceChannel,
					ChannelMode = AudioChannelMode.ReplaceExisting,
					AllowFadeOutOnReplace = false
				}
			});
		_characterSelectVoice = playback.Handle;
		if (!playback.Succeeded || _characterSelectVoice is null)
			Godot.GD.PushWarning($"[JanusSpire2] Character-select voice failed: {playback.Status} {playback.Message}");
	}

	public static void StopCharacterSelectVoice()
	{
		_characterSelectVoice?.TryStop(allowFadeOut: false);
		_characterSelectVoice?.Dispose();
		_characterSelectVoice = null;
		GameFmod.Playback.StopChannel(CharacterSelectVoiceChannel, allowFadeOut: false);
	}

	public static void Register()
	{
		VirtualFmodEventRegistry.RegisterOneShots(new Dictionary<string, string>
		{
			[AttackEvent] = "res://JanusSpire2/sfx/Janus_attacksfx.mp3",
			[CastEvent] = "res://JanusSpire2/sfx/Janus_castsfx.mp3",
			[DeathEvent] = "res://JanusSpire2/sfx/Janus_deathsfx.mp3",
			[CharacterSelectEvent] = CharacterSelectVoiceResource,
			[CharacterTransitionEvent] = "res://JanusSpire2/sfx/Janus_character_transition.mp3",
		}, FmodStudioRouting.SfxBus);
	}
}

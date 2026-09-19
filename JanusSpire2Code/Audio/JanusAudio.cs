using STS2RitsuLib.Audio;

namespace JanusSpire2.JanusSpire2Code.Audio;

public static class JanusAudio
{
	public const string AttackEvent = "event:/JanusSpire2/sfx/attack";
	public const string CastEvent = "event:/JanusSpire2/sfx/cast";
	public const string DeathEvent = "event:/JanusSpire2/sfx/death";
	public const string CharacterSelectEvent = "event:/JanusSpire2/sfx/character_select";
	public const string CharacterTransitionEvent = "event:/JanusSpire2/sfx/character_transition";

	public static void Register()
	{
		VirtualFmodEventRegistry.RegisterOneShots(new Dictionary<string, string>
		{
			[AttackEvent] = "res://JanusSpire2/sfx/Janus_attacksfx.mp3",
			[CastEvent] = "res://JanusSpire2/sfx/Janus_castsfx.mp3",
			[DeathEvent] = "res://JanusSpire2/sfx/Janus_deathsfx.mp3",
			[CharacterSelectEvent] = "res://JanusSpire2/sfx/Janus_character_select.mp3",
			[CharacterTransitionEvent] = "res://JanusSpire2/sfx/Janus_character_transition.mp3",
		}, FmodStudioRouting.SfxBus);
	}
}

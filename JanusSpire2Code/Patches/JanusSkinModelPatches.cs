using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Characters;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib;
using STS2RitsuLib.Patching.Models;

namespace JanusSpire2.JanusSpire2Code.Patches;

/// <summary>
/// Skin variants are persistence identities, not additional gameplay characters.
/// Remove them from global character enumeration while keeping direct ModelDb lookup available.
/// </summary>
public sealed class JanusSkinEnumerationPatch : IPatchMethod
{
	public static string PatchId => "janus_skin_character_enumeration";
	public static string Description => "Exclude auxiliary Janus skin models from global character lists";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(ModelDb), nameof(ModelDb.AllCharacters), null, MethodType.Getter)
	];

	[HarmonyAfter(Const.FrameworkContentRegistryHarmonyId)]
	[HarmonyPriority(Priority.Last)]
	[HarmonyPostfix]
	public static void Postfix(ref IEnumerable<CharacterModel> __result)
	{
		__result = __result.Where(character =>
			character is not JanusCharacter janus || janus.CurrentSkin == JanusSkin.Default);
	}
}

public static class JanusSharedProgression
{
	public static ModelId GetProgressionId(ModelId characterId)
	{
		return IsSkinVariantId(characterId) ? ModelDb.GetId<JanusCharacter>() : characterId;
	}

	private static bool IsSkinVariantId(ModelId characterId)
	{
		return characterId == ModelDb.GetId<JanusVariantThree>()
		       || characterId == ModelDb.GetId<JanusVariantFour>()
		       || characterId == ModelDb.GetId<JanusVariantFive>()
		       || characterId == ModelDb.GetId<JanusVariantSix>()
		       || characterId == ModelDb.GetId<JanusVariantSeven>()
		       || characterId == ModelDb.GetId<JanusVariantH>();
	}
}

public sealed class JanusSharedProgressionLookupPatch : IPatchMethod
{
	public static string PatchId => "janus_skin_shared_progression";
	public static string Description => "Share Janus progression across Spine skins";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(ProgressState), nameof(ProgressState.GetOrCreateCharacterStats), [typeof(ModelId)]),
		new(typeof(ProgressState), nameof(ProgressState.GetStatsForCharacter), [typeof(ModelId)])
	];

	[HarmonyPrefix]
	public static void Prefix(ref ModelId characterId)
	{
		characterId = JanusSharedProgression.GetProgressionId(characterId);
	}
}

/// <summary>
/// Skin variants use distinct model IDs for save/load identity, while their gameplay identity remains Janus.
/// Ancient dialogue lookup is keyed by the exact character entry, so normalize only that lookup to the
/// base Janus entry. This lets every skin reuse Janus-specific dialogue without duplicating localization keys.
/// </summary>
public sealed class JanusSkinAncientDialogueLookupPatch : IPatchMethod
{
	public static string PatchId => "janus_skin_ancient_dialogue_lookup";
	public static string Description => "Use base Janus dialogue for every Janus skin variant";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(AncientDialogueSet), nameof(AncientDialogueSet.GetValidDialogues),
			[typeof(ModelId), typeof(int), typeof(int), typeof(bool)])
	];

	[HarmonyPrefix]
	public static void Prefix(ref ModelId characterId)
	{
		characterId = JanusSharedProgression.GetProgressionId(characterId);
	}
}

public sealed class JanusSharedGameOverProgressionPatch : IPatchMethod
{
	public static string PatchId => "janus_skin_shared_game_over_progression";
	public static string Description => "Store Janus skin badges in shared character progression";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NGameOverScreen), "SaveBadgesToProgress", null)
	];

	/// <summary>
	/// Temporarily aliases the selected skin ID to Janus's shared CharacterStats entry.
	/// This lets the complete official badge-saving method (and other mods' patches on it)
	/// keep running without rewriting its dictionary-indexer IL.
	/// </summary>
	[HarmonyPrefix]
	[HarmonyPriority(Priority.First)]
	public static void Prefix(Player ____localPlayer, out IDisposable? __state)
	{
		__state = null;
		ModelId skinId = ____localPlayer.Character.Id;
		ModelId progressionId = JanusSharedProgression.GetProgressionId(skinId);
		if (skinId == progressionId)
		{
			return;
		}

		ProgressState progress = SaveManager.Instance.Progress;
		if (progress.CharacterStats is not IDictionary<ModelId, CharacterStats> characterStats)
		{
			throw new InvalidOperationException(
				"The game-over progression dictionary is not mutable; Janus cannot install its scoped skin alias.");
		}

		CharacterStats sharedStats = progress.GetOrCreateCharacterStats(progressionId);
		bool hadPrevious = characterStats.TryGetValue(skinId, out CharacterStats? previous);
		characterStats[skinId] = sharedStats;
		__state = new CharacterStatsAliasLease(characterStats, skinId, hadPrevious, previous);
	}

	public static void Finalizer(IDisposable? __state)
	{
		__state?.Dispose();
	}

	private sealed class CharacterStatsAliasLease(
		IDictionary<ModelId, CharacterStats> characterStats,
		ModelId skinId,
		bool hadPrevious,
		CharacterStats? previous) : IDisposable
	{
		public void Dispose()
		{
			if (hadPrevious)
			{
				characterStats[skinId] = previous!;
			}
			else
			{
				characterStats.Remove(skinId);
			}
		}
	}
}

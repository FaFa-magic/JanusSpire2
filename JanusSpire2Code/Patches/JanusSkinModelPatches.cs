using System.Reflection.Emit;
using HarmonyLib;
using JanusSpire2.JanusSpire2Code.Characters;
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

public sealed class JanusSharedGameOverProgressionPatch : IPatchMethod
{
	public static string PatchId => "janus_skin_shared_game_over_progression";
	public static string Description => "Store Janus skin badges in shared character progression";
	public static bool IsCritical => true;
	public static ModPatchTarget[] GetTargets() =>
	[
		new(typeof(NGameOverScreen), "SaveBadgesToProgress", null)
	];

	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var dictionaryIndexer = AccessTools.PropertyGetter(
			typeof(IReadOnlyDictionary<ModelId, CharacterStats>),
			"Item");
		var sharedIndexer = AccessTools.DeclaredMethod(
			typeof(JanusSharedGameOverProgressionPatch),
			nameof(GetSharedCharacterStats));
		var replaced = false;

		foreach (var instruction in instructions)
		{
			if (instruction.Calls(dictionaryIndexer))
			{
				replaced = true;
				var replacement = new CodeInstruction(OpCodes.Call, sharedIndexer);
				replacement.labels.AddRange(instruction.labels);
				replacement.blocks.AddRange(instruction.blocks);
				yield return replacement;
				continue;
			}

			yield return instruction;
		}

		if (!replaced)
			MainFile.Logger.Error("Unable to patch the game-over Janus skin progression lookup.");
	}

	private static CharacterStats GetSharedCharacterStats(
		IReadOnlyDictionary<ModelId, CharacterStats> stats,
		ModelId characterId)
	{
		return stats[JanusSharedProgression.GetProgressionId(characterId)];
	}
}

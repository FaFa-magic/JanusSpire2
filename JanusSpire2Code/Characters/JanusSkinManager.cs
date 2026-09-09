namespace JanusSpire2.JanusSpire2Code.Characters;

public enum JanusSkin
{
	Default,
	VariantThree,
	VariantFour,
	VariantFive,
	VariantSix,
	VariantSeven,
	VariantH
}

public sealed record JanusSkinDefinition(
	JanusSkin Id,
	string SpineSkeletonDataPath,
	string MerchantAnimPath,
	string RestSiteAnimPath);

public static class JanusSkinManager
{
	private const string AnimationRoot = "res://JanusSpire2/animations/characters/Janus";
	private const string SceneRoot = "res://JanusSpire2/scenes/characters";

	private static readonly IReadOnlyDictionary<JanusSkin, JanusSkinDefinition> Skins =
		new Dictionary<JanusSkin, JanusSkinDefinition>
		{
			[JanusSkin.Default] = Create(JanusSkin.Default, "yanusi", ""),
			[JanusSkin.VariantThree] = Create(JanusSkin.VariantThree, "yanusi_3", "_3"),
			[JanusSkin.VariantFour] = Create(JanusSkin.VariantFour, "yanusi_4", "_4"),
			[JanusSkin.VariantFive] = Create(JanusSkin.VariantFive, "yanusi_5", "_5"),
			[JanusSkin.VariantSix] = Create(JanusSkin.VariantSix, "yanusi_6", "_6"),
			[JanusSkin.VariantSeven] = Create(JanusSkin.VariantSeven, "yanusi_7", "_7"),
			[JanusSkin.VariantH] = Create(JanusSkin.VariantH, "yanusi_h", "_h")
		};

	public static JanusSkinDefinition GetDefinition(JanusSkin skin) => Skins[skin];

	private static JanusSkinDefinition Create(JanusSkin id, string resourceName, string sceneSuffix)
	{
		return new(
			id,
			$"{AnimationRoot}/{resourceName}_spine.tres",
			$"{SceneRoot}/janus_merchant{sceneSuffix}.tscn",
			$"{SceneRoot}/janus_rest_site{sceneSuffix}.tscn");
	}
}

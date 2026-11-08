using Godot;
using FileAccess = Godot.FileAccess;

namespace JanusSpire2.JanusSpire2Code.Nodes.UI;

/// <summary>Resource/localization contract shared with the standalone Godot view.</summary>
public static class JanusBattleRewardsAssets
{
    public const string ScenePath = "res://JanusSpire2/scenes/ui/janus_battle_rewards.tscn";
    public const string ButtonPath = "res://JanusSpire2/images/ui/battle_rewards/proceed_button.svg";
    public const string OutlinePath = "res://JanusSpire2/images/ui/battle_rewards/proceed_outline.svg";
    public const string LocPrefix = "JANUS_SPIRE2-BATTLE_REWARDS.";
    public const string BackgroundLayoutPath = "res://JanusSpire2/images/ui/battle_rewards/background_layout.tres";
    public static readonly Vector2 RewardOrigin = new(240f, 226f);
    public const float RewardWindowScale = 0.96f;
    public static readonly string[] PaintingPaths = [
        "res://JanusSpire2/images/ui/battle_rewards/yanusi_painting.png",
        "res://JanusSpire2/images/ui/battle_rewards/yanusi_3_painting.png",
        "res://JanusSpire2/images/ui/battle_rewards/yanusi_4_painting.png",
        "res://JanusSpire2/images/ui/battle_rewards/yanusi_5_painting.png",
        "res://JanusSpire2/images/ui/battle_rewards/yanusi_6_painting.png",
        "res://JanusSpire2/images/ui/battle_rewards/yanusi_7_painting.png",
        "res://JanusSpire2/images/ui/battle_rewards/yanusi_h_painting.png"
    ];

    public static IEnumerable<string> ForSkin(int skin)
    {
        string painting = PaintingPaths[JanusBattleRewardsPolicy.SkinIndex(skin)];
        return [ScenePath, ButtonPath, OutlinePath, BackgroundLayoutPath, painting,
            painting.Replace("_painting.png", "_background.png")];
    }

    public static Dictionary<string, string> LoadPreviewStrings(string language)
    {
        // Start with English so a partial translation cannot break the reward UI.
        var result = ReadStrings("eng");
        if (language != "eng")
            foreach (var pair in ReadStrings(language)) result[pair.Key] = pair.Value;
        return result;
    }

    private static Dictionary<string, string> ReadStrings(string language)
    {
        string path = $"res://JanusSpire2/localization/{language}/gameplay_ui.json";
        if (!FileAccess.FileExists(path)) return [];
        Variant value = Json.ParseString(FileAccess.GetFileAsString(path));
        return value.VariantType == Variant.Type.Dictionary
            ? value.AsGodotDictionary().ToDictionary(pair => pair.Key.AsString(), pair => pair.Value.AsString())
            : [];
    }
}

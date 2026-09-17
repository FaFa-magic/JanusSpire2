using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;

namespace JanusSpire2.JanusSpire2Code.Configs;

public enum FjordMosaicMode
{
    手部模型,
    腿部模型
}

public enum JanusCardFrameMode
{
    卡框一,
    卡框二
}

public sealed class JanusConfig
{
    public FjordMosaicMode 多人模式使用哪种模型 { get; set; } = FjordMosaicMode.手部模型;
    public JanusCardFrameMode 选用哪种卡框 { get; set; } = JanusCardFrameMode.卡框一;
    public bool 会出现先古之民伊丽莎白女王 { get; set; } = true;
    public bool 会出现小猫的卡牌游戏事件 { get; set; } = true;
}

public static class JanusConfigPage
{
    private const string DataKey = "JanusConfig";
    private static readonly I18N Localization = RitsuLibFramework.CreateModLocalization(
        MainFile.ModId,
        "JanusConfig",
        pckFolders: ["res://JanusSpire2/localization/settings"]);

    public static readonly ModSettingsValueBinding<JanusConfig, FjordMosaicMode> ModelModeBinding = new(
        MainFile.ModId, DataKey, SaveScope.Profile,
        static s => s.多人模式使用哪种模型,
        static (s, v) => s.多人模式使用哪种模型 = v);
    public static readonly ModSettingsValueBinding<JanusConfig, JanusCardFrameMode> CardFrameBinding = new(
        MainFile.ModId, DataKey, SaveScope.Profile,
        static s => s.选用哪种卡框,
        static (s, v) => s.选用哪种卡框 = v);
    public static readonly ModSettingsValueBinding<JanusConfig, bool> QueenElizabethEnabledBinding = new(
        MainFile.ModId, DataKey, SaveScope.Profile,
        static s => s.会出现先古之民伊丽莎白女王,
        static (s, v) => s.会出现先古之民伊丽莎白女王 = v);
    public static readonly ModSettingsValueBinding<JanusConfig, bool> KittenCardGameEnabledBinding = new(
        MainFile.ModId, DataKey, SaveScope.Profile,
        static s => s.会出现小猫的卡牌游戏事件,
        static (s, v) => s.会出现小猫的卡牌游戏事件 = v);

    public static void Register()
    {
        ModDataStore.For(MainFile.ModId).Register<JanusConfig>(
            key: DataKey, 
            fileName: "settings.json", 
            scope: SaveScope.Profile,
            defaultFactory: () => new JanusConfig(),
            autoCreateIfMissing: true);

        RitsuLibFramework.RegisterModSettings(MainFile.ModId, page => page
            .WithTitle(Text("config.page.title", "Janus Settings"))
            .WithModDisplayName(Text("config.modDisplayName", "Janus"))
            .WithVisibleOnHostSurfaces(
                ModSettingsHostSurface.MainMenu | ModSettingsHostSurface.RunPause)
            .AddSection("general", section => section
                .WithTitle(Text("config.section.general", "General"))
                .AddChoice("mosaic_mode", Text("config.modelMode.label", "Multiplayer model"),
                    ModelModeBinding,
                    [
                        new(FjordMosaicMode.手部模型, Text("config.modelMode.hand", "Hand model")),
                        new(FjordMosaicMode.腿部模型, Text("config.modelMode.legs", "Leg model"))
                    ],
                    presentation: ModSettingsChoicePresentation.Dropdown)
                .AddChoice("card_frame_mode", Text("config.cardFrame.label", "Custom card frame"),
                    CardFrameBinding,
                    [
                        new(JanusCardFrameMode.卡框一, Text("config.cardFrame.first", "Frame 1")),
                        new(JanusCardFrameMode.卡框二, Text("config.cardFrame.second", "Frame 2"))
                    ],
                    presentation: ModSettingsChoicePresentation.Dropdown)
                .AddToggle(
                    "queen_elizabeth_enabled",
                    Text("config.queenElizabethEnabled.label", "Enable Queen Elizabeth"),
                    QueenElizabethEnabledBinding)
                .AddToggle(
                    "kitten_card_game_enabled",
                    Text("config.kittenCardGameEnabled.label", "Enable A Kitten's Card Game"),
                    KittenCardGameEnabledBinding)));
    }

    private static ModSettingsText Text(string key, string fallback)
    {
        return ModSettingsText.I18N(Localization, key, fallback);
    }
}

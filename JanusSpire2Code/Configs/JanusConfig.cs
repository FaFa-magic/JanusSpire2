using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
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
}

public static class JanusConfigPage
{
    private const string DataKey = "JanusConfig";

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

    public static void Register()
    {
        ModDataStore.For(MainFile.ModId).Register<JanusConfig>(
            key: DataKey, 
            fileName: "settings.json", 
            scope: SaveScope.Profile,
            defaultFactory: () => new JanusConfig(),
            autoCreateIfMissing: true);

        RitsuLibFramework.RegisterModSettings(MainFile.ModId, page => page
            .WithTitle(ModSettingsText.Literal("Janus Config"))
            .WithModDisplayName(ModSettingsText.Literal("Janus Mod"))
            .WithVisibleOnHostSurfaces(
                ModSettingsHostSurface.MainMenu | ModSettingsHostSurface.RunPause)
            .AddSection("general", section => section
                .WithTitle(ModSettingsText.Literal("Janus Config"))
                .AddChoice("mosaic_mode", ModSettingsText.Literal("多人模式模型选择"),
                    ModelModeBinding,
                    [
                        new(FjordMosaicMode.手部模型, ModSettingsText.Literal("手部模型")),
                        new(FjordMosaicMode.腿部模型, ModSettingsText.Literal("腿部模型"))
                    ],
                    presentation: ModSettingsChoicePresentation.Dropdown)
                .AddChoice("card_frame_mode", ModSettingsText.Literal("自定义卡框选择"),
                    CardFrameBinding,
                    [
                        new(JanusCardFrameMode.卡框一, ModSettingsText.Literal("卡框一")),
                        new(JanusCardFrameMode.卡框二, ModSettingsText.Literal("卡框二"))
                    ],
                    presentation: ModSettingsChoicePresentation.Dropdown)
                .AddToggle(
                    "queen_elizabeth_enabled",
                    ModSettingsText.Literal("会出现先古之民-伊丽莎白女王"),
                    QueenElizabethEnabledBinding)));
    }
}

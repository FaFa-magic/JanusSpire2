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

public sealed class JanusConfig
{
    public FjordMosaicMode 多人模式使用哪种模型 { get; set; } = FjordMosaicMode.手部模型;
}

public static class JanusConfigPage
{
    private const string DataKey = "JanusConfig";

    public static readonly ModSettingsValueBinding<JanusConfig, FjordMosaicMode> ModelModeBinding = new(
        MainFile.ModId, DataKey, SaveScope.Profile,
        static s => s.多人模式使用哪种模型,
        static (s, v) => s.多人模式使用哪种模型 = v);

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
                    presentation: ModSettingsChoicePresentation.Dropdown)));
    }
    
    public static void Init()
    {
        
    }
}
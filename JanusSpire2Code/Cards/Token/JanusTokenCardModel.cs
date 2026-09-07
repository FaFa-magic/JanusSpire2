using JanusSpire2.JanusSpire2Code.Configs;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards.Token;

[RegisterCard(typeof(TokenCardPool), Inherit = true)]
public abstract class JanusTokenCardModel : ModCardTemplate
{
    public JanusTokenCardModel(int energyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary = true)
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }
    
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://JanusSpire2/images/cards/{GetType().Name}.png",
        BannerTexturePath: "res://JanusSpire2/images/card_frames/janus_Banner.png",
        AncientBannerPath: "res://JanusSpire2/images/card_frames/janus_Banner.png",
        AncientBorderPath: "res://JanusSpire2/images/card_frames/janus_ancient.png",
        AncientBorderMaterialPath: "res://JanusSpire2/materials/cards/janus_ancient_border_opaque.tres",
        FramePath: Type switch
        {
            CardType.Attack => JanusConfigPage.CardFrameBinding.Read() == JanusCardFrameMode.卡框一
                ? "res://JanusSpire2/images/card_frames/janus_attack_1.png"
                : "res://JanusSpire2/images/card_frames/janus_attack_2.png",
            CardType.Skill => JanusConfigPage.CardFrameBinding.Read() == JanusCardFrameMode.卡框一
                ? "res://JanusSpire2/images/card_frames/janus_skill_1.png"
                : "res://JanusSpire2/images/card_frames/janus_skill_2.png",
            CardType.Power => JanusConfigPage.CardFrameBinding.Read() == JanusCardFrameMode.卡框一
                ? "res://JanusSpire2/images/card_frames/janus_power_1.png"
                : "res://JanusSpire2/images/card_frames/janus_power_2.png",
            _ => ""
        }
    );
}

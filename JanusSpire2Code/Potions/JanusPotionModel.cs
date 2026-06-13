using JanusSpire2.JanusSpire2Code.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Potions;

[RegisterPotion(typeof(JanusPotionPool), Inherit = true)]
public abstract class JanusPotionModel : ModPotionTemplate
{
    public override PotionAssetProfile AssetProfile => new(
        ImagePath: $"res://JanusSpire2/images/potions/big/{GetType().Name}.png",
        OutlinePath: $"res://JanusSpire2/images/potions/outline/{GetType().Name}.png"
    );
}
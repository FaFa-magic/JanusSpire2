using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Powers;

[RegisterPower(Inherit = true)]
public abstract class JanusPowerModel : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://JanusSpire2/images/powers/big/{GetType().Name}.png",
        BigIconPath: $"res://JanusSpire2/images/powers/big/{GetType().Name}.png"
    );
}
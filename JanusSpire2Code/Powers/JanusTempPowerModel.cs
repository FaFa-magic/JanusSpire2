using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Powers;

[RegisterPower(Inherit = true)]
public abstract class JanusTempPowerModel<TOriginModel, TPower> : ModTemporaryAppliedPowerTemplate<TOriginModel, TPower> 
    where TOriginModel : AbstractModel
    where TPower : PowerModel
{
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://JanusSpire2/images/powers/big/{GetType().Name}.png",
        BigIconPath: $"res://JanusSpire2/images/powers/big/{GetType().Name}.png"
    );
    
    // protected override bool IsPositive => false; // 正面效果还是负面

    // protected override bool UntilEndOfOtherSideTurn => false; // 为 true 时，在另一方回合结束时过期；否则在拥有者一方回合结束时过期。

    // protected override int LastForXExtraTurns => 0; // 额外持续回合数
}
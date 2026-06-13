using JanusSpire2.JanusSpire2Code.Cards.Rare;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class BattlePlanCardDebuffPower : JanusPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<BattlePlan>()
    ];
    
    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != base.Owner.Player)
        {
            return count;
        }
        return count - (decimal)base.Amount;
    }
}
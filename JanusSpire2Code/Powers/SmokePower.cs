using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class SmokePower : JanusPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<HiddenAttackPower>()
    ];
    
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target == base.Owner)
        {
            return 0.5m;
        }
        
        return 1m;
    }
    
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == base.Owner && result.TotalDamage != 0)
        {
            Flash();
            await PowerCmd.Decrement(this);
        }
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        await PowerCmd.Apply<HiddenAttackPower>(new ThrowingPlayerChoiceContext(), base.Owner, 50m, base.Owner, null);
        int escapeIntoSmokeAmount = base.Owner.GetPower<EscapeIntoSmokePower>()?.Amount ?? 0;
        if (escapeIntoSmokeAmount > 0)
        {
            await PowerCmd.Apply<SmokePower>(new ThrowingPlayerChoiceContext(), base.Owner, escapeIntoSmokeAmount, base.Owner, null);
        }
    }
}
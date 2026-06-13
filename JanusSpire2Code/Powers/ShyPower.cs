using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class ShyPower : JanusPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    private class Data
    {
        public bool hasGainedBlockThisTurn;
    }
    
    public bool HasGainedBlockThisTurn
    {
        get
        {
            return GetInternalData<Data>().hasGainedBlockThisTurn;
        }
        private set
        {
            AssertMutable();
            GetInternalData<Data>().hasGainedBlockThisTurn = value;
        }
    }

    protected override object InitInternalData()
    {
        return new Data();
    }
    
    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (!HasGainedBlockThisTurn && command.DamageProps.HasFlag(ValueProp.Move))
        {
            DamageResult? damageResult = command.Results.SelectMany((List<DamageResult> r) => r).FirstOrDefault((DamageResult r) => r.Receiver == base.Owner);
            if (damageResult != null && damageResult.UnblockedDamage != 0)
            {
                HasGainedBlockThisTurn = true;
                await CreatureCmd.TriggerAnim(base.Owner, "BlockStart", 0.3f);
                await CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
            }
        }
    }
    
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != base.Owner.Side)
        {
            if (HasGainedBlockThisTurn)
            {
                await CreatureCmd.TriggerAnim(base.Owner, "BlockEnd", 0.15f);
            }
            HasGainedBlockThisTurn = false;
        }
    }
}
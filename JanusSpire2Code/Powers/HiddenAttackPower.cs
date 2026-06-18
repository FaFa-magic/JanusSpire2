using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class HiddenAttackPower : JanusPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    private class Data
    {
        public AttackCommand? commandToModify;
    }

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task BeforeAttack(AttackCommand command)
    {
        if (!(command.ModelSource is CardModel cardModel))
        {
            return Task.CompletedTask;
        }
        if (cardModel.Owner.Creature != base.Owner)
        {
            return Task.CompletedTask;
        }
        if (cardModel.Type != CardType.Attack)
        {
            return Task.CompletedTask;
        }
        if (!command.DamageProps.IsPoweredAttack())
        {
            return Task.CompletedTask;
        }
        Data internalData = GetInternalData<Data>();
        if (internalData.commandToModify != null)
        {
            return Task.CompletedTask;
        }
        internalData.commandToModify = command;
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (cardSource == null)
        {
            return 1m;
        }
        if (cardSource.Owner.Creature != base.Owner)
        {
            return 1m;
        }
        if (!props.IsPoweredAttack())
        {
            return 1m;
        }
        Data internalData = GetInternalData<Data>();
        if (internalData.commandToModify == null || cardSource == internalData.commandToModify.ModelSource)
        {
            return 1m + (decimal)base.Amount / 100m;
        }
        return 1m;
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        Data internalData = GetInternalData<Data>();
        if (command == internalData.commandToModify)
        {
            internalData.commandToModify = null;
            await PowerCmd.Remove(this);
        }
    }
}
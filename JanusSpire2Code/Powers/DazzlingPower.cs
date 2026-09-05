using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class DazzlingPower : JanusPowerModel
{
    private sealed record PendingConversion(decimal Amount);

    private sealed class Data
    {
        public Dictionary<PowerModel, PendingConversion> PendingConversions { get; } = [];
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData() => new Data();

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (target != Owner ||
            amount == 0M ||
            canonicalPower is BlackCatSealPower ||
            !canonicalPower.IsVisible ||
            canonicalPower.GetTypeForAmount(amount) != PowerType.Debuff)
        {
            return false;
        }

        GetInternalData<Data>().PendingConversions[canonicalPower] =
            new PendingConversion(Math.Abs(amount));
        modifiedAmount = 0M;
        return true;
    }

    public override async Task AfterModifyingPowerAmountReceived(PowerModel power)
    {
        Data data = GetInternalData<Data>();
        if (!data.PendingConversions.Remove(power, out PendingConversion? conversion))
        {
            return;
        }

        Flash();
        await PowerCmd.Decrement(this);

        if (Owner.IsDead || conversion.Amount <= 0M)
        {
            return;
        }

        await PowerCmd.Apply<BlackCatSealPower>(
            new ThrowingPlayerChoiceContext(),
            Owner,
            conversion.Amount,
            Owner,
            null);
    }

    /// <summary>
    /// Tender represents one temporary stat-loss effect as two independent power
    /// applications and records a shared amount to restore at end of turn.  Going
    /// through the ordinary modifier hook would therefore only replace the first
    /// half and would leave both the other half and the later restoration behind.
    /// Treat that pair as one debuff transaction instead.
    /// </summary>
    internal async Task ConvertTenderPenalty(PlayerChoiceContext choiceContext)
    {
        if (Amount <= 0 || Owner.IsDead)
        {
            return;
        }

        Flash();
        await PowerCmd.Decrement(this);

        if (Owner.IsDead)
        {
            return;
        }

        await PowerCmd.Apply<BlackCatSealPower>(
            choiceContext,
            Owner,
            1M,
            Owner,
            null);
    }
}

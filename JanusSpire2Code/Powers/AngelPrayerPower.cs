using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class AngelPrayerPower : JanusPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldPlayerResetEnergy(Player player)
    {
        if (player != base.Owner.Player && this.Amount > 0)
        {
            return true;
        }
        return false;
    }
    
    public override bool ShouldTakeExtraTurn(Player player)
    {
        if (player == base.Owner.Player && this.Amount > 0)
        {
            return true;
        }
        return false;
    }

    public override async Task AfterTakingExtraTurn(Player player)
    {
        if (player == base.Owner.Player && this.Amount > 0)
        {
            this.Flash();
            await PowerCmd.Decrement(this);
        }
        await base.AfterTakingExtraTurn(player);
    }
}
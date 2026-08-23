using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class MagicWand : JanusRelicModel
{
    private readonly HashSet<PowerModel> _activeTemporaryDebuffs = [];

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override decimal ModifyPowerAmountGivenAdditive(
        PowerModel power,
        Creature giver,
        decimal amount,
        Creature? target,
        CardModel? cardSource)
    {
        if (giver != Owner.Creature ||
            target == null ||
            target.Side == Owner.Creature.Side ||
            amount == 0M ||
            power.GetTypeForAmount(amount) != PowerType.Debuff ||
            IsInternallyAppliedByActiveTemporaryDebuff(power))
        {
            return 0M;
        }

        if (power is ITemporaryPower)
        {
            _activeTemporaryDebuffs.Add(power);
        }

        return Math.Sign(amount);
    }

    public override Task AfterModifyingPowerAmountGiven(PowerModel power)
    {
        _activeTemporaryDebuffs.Remove(power);
        Flash();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _activeTemporaryDebuffs.Clear();
        return Task.CompletedTask;
    }

    private bool IsInternallyAppliedByActiveTemporaryDebuff(PowerModel power)
    {
        Type powerType = power.GetType();
        return _activeTemporaryDebuffs
            .OfType<ITemporaryPower>()
            .Any(temporaryPower => temporaryPower.InternallyAppliedPower.GetType() == powerType);
    }
}

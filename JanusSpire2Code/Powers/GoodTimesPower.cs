using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Combat.HandSize;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class GoodTimesPower : JanusPowerModel, IMaxHandSizeModifier
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public int ModifyMaxHandSize(Player player, int currentMaxHandSize)
    {
        return player == Owner.Player
            ? currentMaxHandSize + Amount
            : currentMaxHandSize;
    }
}

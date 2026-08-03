using MegaCrit.Sts2.Core.Entities.Powers;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class AHurriedDayPower : JanusPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
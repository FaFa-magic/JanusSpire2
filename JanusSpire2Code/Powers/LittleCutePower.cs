using MegaCrit.Sts2.Core.Entities.Powers;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class LittleCutePower : JanusPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
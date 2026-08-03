using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interactions.RightClick;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class DrawAfterClickPower : JanusPowerModel, IModRightClickablePower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        if (this.Owner.Player != null && context.PlayerChoiceContext != null)
        {
            await CardPileCmd.Draw(context.PlayerChoiceContext, 1, base.Owner.Player);
            await PowerCmd.Decrement(this);
        }
    }
}
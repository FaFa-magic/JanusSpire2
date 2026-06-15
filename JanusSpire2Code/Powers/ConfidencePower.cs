using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class ConfidencePower : JanusPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override async Task AfterHandEmptied(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.PlayerCombatState != null && IsValidPhase(player.PlayerCombatState.Phase))
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, player);
        }
    }

    private static bool IsValidPhase(PlayerTurnPhase phase)
    {
        if ((uint)(phase - 2) <= 2u)
        {
            return true;
        }
        return false;
    }
}
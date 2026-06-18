using JanusSpire2.JanusSpire2Code.Patches;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class ConfidencePower : JanusPowerModel, IAfterHandReducedHook
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.PlayerCombatState != null && player.Creature == base.Owner && IsValidPhase(player.PlayerCombatState.Phase))
        {
            await TriggerDrawLogic(choiceContext, player);
        }
    }
    
    public async Task AfterHandReduced(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.PlayerCombatState != null && player.Creature == base.Owner && IsValidPhase(player.PlayerCombatState.Phase))
        {
            await TriggerDrawLogic(choiceContext, player);
        }
    }
    
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == CombatSide.Enemy && base.Owner.Player != null)
        {
            await TriggerDrawLogic(choiceContext, base.Owner.Player);
        }
    }

    private async Task TriggerDrawLogic(PlayerChoiceContext choiceContext, Player player)
    {
        while (player.PlayerCombatState != null && player.PlayerCombatState.Hand.Cards.Count < this.Amount)
        {
            if (player.PlayerCombatState.DrawPile.Cards.Count == 0 && 
                player.PlayerCombatState.DiscardPile.Cards.Count == 0)
            {
                break;
            }

            Flash();
            await CardPileCmd.Draw(choiceContext, player); 
            await Task.Yield();
        }
    }

    private static bool IsValidPhase(PlayerTurnPhase phase)
    {
        return (uint)(phase - 2) <= 2u;
    }
}
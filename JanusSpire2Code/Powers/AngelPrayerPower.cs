using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;

using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class AngelPrayerPower : JanusPowerModel
{
    private bool _isTakingGrantedExtraTurn;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    [SavedProperty]
    public bool IsTakingGrantedExtraTurn
    {
        get => _isTakingGrantedExtraTurn;
        private set
        {
            AssertMutable();
            _isTakingGrantedExtraTurn = value;
        }
    }

    public override bool ShouldPlayerResetEnergy(Player player)
    {
        return player != Owner.Player || !IsTakingGrantedExtraTurn || Amount <= 0;
    }
    
    public override bool ShouldTakeExtraTurn(Player player)
    {
        return player == Owner.Player && Amount > 0;
    }

    public override Task AfterTakingExtraTurn(Player player)
    {
        if (player == Owner.Player && Amount > 0)
        {
            Flash();
            IsTakingGrantedExtraTurn = true;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player ||
            !IsTakingGrantedExtraTurn ||
            !participants.Contains(Owner))
        {
            return;
        }

        IsTakingGrantedExtraTurn = false;
        await PowerCmd.Decrement(this);
    }
}

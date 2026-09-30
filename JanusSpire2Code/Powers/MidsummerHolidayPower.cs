using MegaCrit.Sts2.Core.Combat;
using JanusSpire2.JanusSpire2Code.Cards;
using JanusSpire2.JanusSpire2Code.Cards.Rare;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class MidsummerHolidayPower : JanusPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        RefreshRipplingBlueWaves(Owner);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        await base.AfterRemoved(oldOwner);
        RefreshRipplingBlueWaves(oldOwner);
    }

    private static void RefreshRipplingBlueWaves(Creature creature)
    {
        if (creature.Player?.PlayerCombatState?.AllCards is not { } cards)
            return;

        foreach (CardModel card in cards)
        {
            if (card is RipplingBlueWaves)
                card.RequestTextRefresh();
        }
    }
    
    public override async Task AfterSideTurnEndLate(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy)
        {
            await PowerCmd.Remove(this);
        }
    }
}

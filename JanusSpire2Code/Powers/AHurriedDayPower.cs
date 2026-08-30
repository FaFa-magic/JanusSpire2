using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class AHurriedDayPower : JanusPowerModel
{
    private sealed class Data
    {
        public readonly Dictionary<CardModel, int> DamageByPlayedCard = new();
    }

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == Owner)
        {
            GetInternalData<Data>().DamageByPlayedCard.Add(cardPlay.Card, Amount);
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!GetInternalData<Data>().DamageByPlayedCard.Remove(cardPlay.Card, out int damage))
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(
            choiceContext,
            Owner,
            damage,
            ValueProp.Unpowered,
            null,
            null);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}

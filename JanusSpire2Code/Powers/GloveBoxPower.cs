using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class GloveBoxPower : JanusPowerModel
{
    private class Data
    {
        public int cardsPlayed;
        public int triggerCount;
    }

    private const int CardIncrement = 4;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => CardIncrement - GetInternalData<Data>().cardsPlayed % CardIncrement;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("CardIncrement", CardIncrement)];

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner || !cardPlay.Card.Keywords.Contains(JanusKeywords.Sticker))
            return;

        var data = GetInternalData<Data>();

        data.cardsPlayed++;

        int triggers = data.cardsPlayed / CardIncrement - data.triggerCount;
        if (triggers > 0 && Owner.Player != null)
        {
            Flash();
            CardModel? cardModel = CardFactory.GetDistinctForCombat(
                    Owner.Player,
                    from c in Owner.Player.Character.CardPool.GetUnlockedCards(
                        Owner.Player.UnlockState,
                        Owner.Player.RunState.CardMultiplayerConstraint)
                    where cardPlay.Card.Keywords.Contains(JanusKeywords.Sticker)
                    select c,
                    1,
                    Owner.Player.RunState.Rng.CombatCardGeneration)
                .FirstOrDefault();

            if (cardModel != null)
            {
                cardModel.SetToFreeThisTurn();
                await CardPileCmd.AddGeneratedCardToCombat(cardModel, PileType.Hand, Owner.Player);
            }
            data.triggerCount += triggers;
        }

        InvokeDisplayAmountChanged();
    }
}
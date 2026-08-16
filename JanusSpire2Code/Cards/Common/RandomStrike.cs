using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Common;

public sealed class RandomStrike() : JanusCardModel(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6M, ValueProp.Move),
        new CardsVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardPoolModel> list = base.Owner.UnlockState.CharacterCardPools.ToList();

        IEnumerable<CardModel> cards = from c in list.SelectMany((CardPoolModel c) => c.GetUnlockedCards(base.Owner.UnlockState, base.Owner.RunState.CardMultiplayerConstraint))
            where c.Type == CardType.Attack
            select c;
        List<CardModel> cardsToPlay = CardFactory.GetDistinctForCombat(base.Owner, cards, DynamicVars.Cards.IntValue, base.Owner.RunState.Rng.CombatCardGeneration).ToList();
        
        if (cardsToPlay.Count > 0)
        {
            if (base.IsUpgraded)
            {
                foreach (CardModel item in cardsToPlay)
                {
                    CardCmd.Upgrade(item);
                }
            }
            await CardPileCmd.AddGeneratedCardsToCombat(cardsToPlay, PileType.Play, base.Owner);
        }

        foreach (CardModel item in cardsToPlay)
        {
            if (!base.Owner.Creature.IsDead)
            {
                item.ExhaustOnNextPlay = true; 
                await CardCmd.AutoPlay(choiceContext, item, null);
            }
            else
            {
                break;
            }
        }
    }
}
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class AHurriedDay() : JanusCardModel(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (base.Owner.PlayerCombatState != null)
        {
            int num = CardPile.MaxCardsInHand - base.Owner.PlayerCombatState.Hand.Cards.Count;
            await CardPileCmd.Draw(choiceContext, num, base.Owner);
            await PowerCmd.Apply<AHurriedDayPower>(choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);
        }
    }
    
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class Perseverance() : JanusCardModel(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel card = CreateClone();
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, MainFile.Diary, base.Owner), 0.2f);
    }
    
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (base.Owner.Creature.IsDead || card.Owner != base.Owner || this.Pile?.Type != MainFile.Diary || card.Pile?.Type != MainFile.Diary)
        {
            return;
        }

        List<CardModel> cards = CardFactory.GetForCombat(base.Owner, from c in base.Owner.Character.CardPool.GetUnlockedCards(base.Owner.UnlockState, base.Owner.RunState.CardMultiplayerConstraint) 
            select c, 1, base.Owner.RunState.Rng.CombatCardGeneration).ToList();
        
        foreach (CardModel Card in cards)
        {
            await CardPileCmd.AddGeneratedCardToCombat(Card, PileType.Hand, base.Owner);
        }
    }
    
    protected override void OnUpgrade() => base.EnergyCost.UpgradeBy(-1);
}
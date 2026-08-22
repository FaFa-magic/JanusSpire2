using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using JanusSpire2.JanusSpire2Code.Keywords;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class Perseverance() : JanusCardModel(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Transcribe];

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

        CardModel? randomCard = CardFactory.GetForCombat(
            Owner,
            Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint),
            1,
            Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();

        if (randomCard != null)
        {
            await CardPileCmd.AddGeneratedCardToCombat(randomCard, PileType.Hand, Owner);
        }
    }
    
    protected override void OnUpgrade() => base.EnergyCost.UpgradeBy(-1);
}
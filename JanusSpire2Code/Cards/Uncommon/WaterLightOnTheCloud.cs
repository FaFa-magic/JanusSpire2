using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class WaterLightOnTheCloud() : JanusCardModel(1, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5M, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> cardsToRecord = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card.Type != CardType.Skill)
            .ToList();
        List<CardModel> recordedCards = new(cardsToRecord.Count);
        
        foreach (CardModel card in cardsToRecord)
        {
            if (card.Pile?.Type != PileType.Hand)
            {
                continue;
            }

            await CardPileCmd.Add(card, MainFile.Diary);
            recordedCards.Add(card);
        }

        if (recordedCards.Count > 0 && CombatState is { } combatState)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(recordedCards.Count)
                .FromCard(this, cardPlay)
                .TargetingRandomOpponents(combatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2M);
}

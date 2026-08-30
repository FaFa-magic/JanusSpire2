using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class RemedyStrike() : JanusCardModel(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9M, ValueProp.Move),
        new CardsVar(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(JanusKeywords.Record)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        List<CardPoolModel> cardPools = Owner.UnlockState.CharacterCardPools.ToList();
        if (cardPools.Count > 1)
        {
            cardPools.Remove(Owner.Character.CardPool);
        }

        IEnumerable<CardModel> candidates = cardPools.SelectMany(pool =>
            pool.GetUnlockedCards(
                Owner.UnlockState,
                Owner.RunState.CardMultiplayerConstraint));
        List<CardModel> generatedCards = CardFactory.GetForCombat(
            Owner,
            candidates,
            DynamicVars.Cards.IntValue,
            Owner.RunState.Rng.CombatCardGeneration).ToList();

        foreach (CardModel generatedCard in generatedCards)
        {
            if (IsUpgraded)
            {
                CardCmd.Upgrade(generatedCard);
            }

            generatedCard.AddKeyword(JanusKeywords.Record);
            generatedCard.SetToFreeThisTurn();
        }

        await CardPileCmd.AddGeneratedCardsToCombat(generatedCards, PileType.Hand, Owner);
    }
    
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4M);
}

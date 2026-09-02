using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Common;

public sealed class TheLittlestRebel() : JanusCardModel(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(12M, ValueProp.Move)];
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        CardModel? cardToCopy = SelectCardToCopy();
        if (cardToCopy != null)
        {
            CardModel copy = cardToCopy.CreateClone();
            copy.ExhaustOnNextPlay = true;
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Play, Owner);
            await CardCmd.AutoPlay(choiceContext, copy, null);
        }
    }

    private CardModel? SelectCardToCopy()
    {
        IReadOnlyList<CardModel> drawPile = PileType.Draw.GetPile(Owner).Cards;
        if (drawPile.Count == 0)
        {
            return null;
        }

        List<CardModel> candidates = drawPile
            .Where(card => card.Rarity != CardRarity.Basic &&
                           card.Type is CardType.Attack or CardType.Skill or CardType.Power)
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = drawPile
                .Where(card => card.Rarity == CardRarity.Basic)
                .ToList();
        }

        return Owner.RunState.Rng.CombatCardSelection.NextItem(
            candidates.Count > 0 ? candidates : drawPile);
    }
    
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6M);
}

using MegaCrit.Sts2.Core.Commands;
using JanusSpire2.JanusSpire2Code.Singleton;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
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

        if (CombatManager.Instance.IsOverOrEnding || Owner.Creature.IsDead)
            return;

        var combatState = CombatState
            ?? throw new InvalidOperationException("RandomStrike must be played during combat.");
        CardMultiplayerConstraint runConstraint = Owner.RunState.CardMultiplayerConstraint;
        IEnumerable<CardModel> strikeCards = JanusRandomGenerationPool.Filter(ModelDb.AllCards, Owner.RunState).Where(card =>
            card.Tags.Contains(CardTag.Strike)
            && (card.MultiplayerConstraint == CardMultiplayerConstraint.None
                || card.MultiplayerConstraint == runConstraint)
            && (card.Rarity == CardRarity.Basic
                || (card.CanBeGeneratedInCombat
                    && card.Rarity != CardRarity.Ancient
                    && card.Rarity != CardRarity.Event)));

        List<CardModel> cardsToPlay = strikeCards
            .Distinct()
            .TakeRandom(DynamicVars.Cards.IntValue, Owner.RunState.Rng.CombatCardGeneration)
            .Select(card => combatState.CreateCard(card, Owner))
            .ToList();

        if (IsUpgraded)
        {
            foreach (CardModel card in cardsToPlay)
            {
                CardCmd.Upgrade(card);
            }
        }

        IReadOnlyList<CardPileAddResult> generated = await CardPileCmd.AddGeneratedCardsToCombat(
            cardsToPlay, PileType.Play, Owner);
        foreach (CardPileAddResult result in generated)
        {
            if (Owner.Creature.IsDead)
                break;

            if (result.success)
                await JanusSingleton.AutoPlayGeneratedCardAndExhaust(choiceContext, result.cardAdded);
        }
    }
}

using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class UnableToMove() : JanusCardModel(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Counterattack];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardPoolModel> cardPools = Owner.UnlockState.CharacterCardPools.ToList();
        if (cardPools.Count > 1)
        {
            cardPools.Remove(Owner.Character.CardPool);
        }

        IEnumerable<CardModel> candidates = cardPools
            .SelectMany(pool => pool.GetUnlockedCards(
                Owner.UnlockState,
                Owner.RunState.CardMultiplayerConstraint))
            .Where(card => card.Type == CardType.Attack);

        CardModel? generatedCard = CardFactory.GetDistinctForCombat(
                Owner,
                candidates,
                DynamicVars.Cards.IntValue,
                Owner.RunState.Rng.CombatCardGeneration)
            .FirstOrDefault();
        if (generatedCard == null)
        {
            return;
        }

        if (IsUpgraded)
        {
            CardCmd.Upgrade(generatedCard);
        }

        generatedCard.ExhaustOnNextPlay = true;
        await CardPileCmd.AddGeneratedCardToCombat(generatedCard, PileType.Play, Owner);
        await CardCmd.AutoPlay(choiceContext, generatedCard, null);
    }
}

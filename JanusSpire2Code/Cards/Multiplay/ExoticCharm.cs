using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Multiplay;

public sealed class ExoticCharm() : JanusCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(JanusKeywords.Record)
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(3)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        Player? ally = cardPlay.Target.Player;
        if (ally == null || ally.Creature.IsDead)
        {
            return;
        }

        List<CardModel> options = CardFactory.GetDistinctForCombat(
            Owner,
            ally.Character.CardPool.GetUnlockedCards(
                ally.UnlockState,
                ally.RunState.CardMultiplayerConstraint),
            DynamicVars.Cards.IntValue,
            Owner.RunState.Rng.CombatCardGeneration).ToList();
        if (options.Count == 0)
        {
            return;
        }

        if (IsUpgraded)
        {
            foreach (CardModel option in options)
            {
                CardCmd.Upgrade(option);
            }
        }

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            options,
            Owner);
        if (selected == null)
        {
            return;
        }

        selected.AddKeyword(JanusKeywords.Record);
        selected.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
    }
}

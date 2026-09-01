using JanusSpire2.JanusSpire2Code.Cards.Status;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class RipplingBlueWaves() : JanusCardModel(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    private const string CalculatedHitsKey = "CalculatedHits";

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromCard<Wave>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5M, ValueProp.Move),
        new CalculationBaseVar(0M),
        new CalculationExtraVar(1M),
        new CalculatedVar(CalculatedHitsKey)
            .WithMultiplier((card, _) => CalculateExpectedHits(card))
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);

        int emptyHandSlots = CardPile.MaxCardsInHand - PileType.Hand.GetPile(Owner).Cards.Count;
        List<CardModel> waves = new(emptyHandSlots);
        for (int i = 0; i < emptyHandSlots; i++)
        {
            waves.Add(CombatState.CreateCard<Wave>(Owner));
        }

        await CardPileCmd.AddGeneratedCardsToCombat(waves, PileType.Hand, Owner);
        
        List<CardModel> statuses = GetStatuses(Owner).ToList();
        int hitCount = (int)((CalculatedVar)DynamicVars[CalculatedHitsKey])
            .Calculate(cardPlay.Target);
        foreach (CardModel status in statuses)
        {
            await CardCmd.Exhaust(choiceContext, status);
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hitCount)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    private static IEnumerable<CardModel> GetStatuses(Player owner)
    {
        IEnumerable<CardModel> allCards = owner.PlayerCombatState?.AllCards ?? [];
        return allCards.Where(card =>
            card is not JanusRecordMappingCard &&
            card.Type == CardType.Status &&
            card.Pile?.Type != PileType.Exhaust);
    }

    private static int CalculateExpectedHits(CardModel card)
    {
        CardPile hand = PileType.Hand.GetPile(card.Owner);
        int handCountAfterPlaying = hand.Cards.Count;
        if (ReferenceEquals(card.Pile, hand))
        {
            handCountAfterPlaying--;
        }

        int wavesToGenerate = Math.Max(0, CardPile.MaxCardsInHand - handCountAfterPlaying);
        return GetStatuses(card.Owner).Count() + wavesToGenerate;
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2M);
}

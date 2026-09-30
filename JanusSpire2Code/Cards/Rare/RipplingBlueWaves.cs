using JanusSpire2.JanusSpire2Code.Cards.Status;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.HandSize;

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

        int emptyHandSlots = GetEmptyHandSlots(Owner);
        List<CardModel> waves = new(emptyHandSlots);
        for (int i = 0; i < emptyHandSlots; i++)
        {
            waves.Add(CombatState.CreateCard<Wave>(Owner));
        }

        if (waves.Count > 0)
            await CardPileCmd.AddGeneratedCardsToCombat(waves, PileType.Hand, Owner);
        
        List<CardModel> statuses = GetStatuses(Owner).ToList();
        int hitCount = 0;
        foreach (CardModel status in statuses)
        {
            if ((await CardCmd.Exhaust(choiceContext, status))?.success == true)
            {
                hitCount++;
            }
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
        int wavesToGenerate = card.Owner.Creature.GetPower<MidsummerHolidayPower>() == null
            ? GetEmptyHandSlots(card.Owner, card)
            : 0;
        return GetStatuses(card.Owner).Count() + wavesToGenerate;
    }

    private static int GetEmptyHandSlots(
        Player owner,
        CardModel? cardLeavingHand = null)
    {
        CardPile hand = PileType.Hand.GetPile(owner);
        int currentHandSize = hand.Cards.Count;
        if (ReferenceEquals(cardLeavingHand?.Pile, hand))
        {
            currentHandSize--;
        }

        return Math.Max(0, MaxHandSizeCalculator.Calculate(owner) - currentHandSize);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2M);
}

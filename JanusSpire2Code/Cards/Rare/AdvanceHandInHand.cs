using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using JanusSpire2.JanusSpire2Code.Keywords;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class AdvanceHandInHand() : JanusCardModel(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ModCardVars.Int(InspirationKeyword.AmountVar, 1),
        new CalculationBaseVar(0M),
        new ExtraDamageVar(1M),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(CountDistinctCardNames)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    private static decimal CountDistinctCardNames(CardModel card, Creature? _)
    {
        return card.Owner.PlayerCombatState!.AllCards
            .Where(otherCard => otherCard is not JanusRecordMappingCard)
            .Select(otherCard => otherCard.Id)
            .Distinct()
            .Count();
    }
    
    protected override void OnUpgrade()
    {
        AddKeyword(JanusKeywords.Inspiration);
    }
}

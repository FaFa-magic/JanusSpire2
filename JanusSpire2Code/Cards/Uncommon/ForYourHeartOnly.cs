using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class ForYourHeartOnly() : JanusCardModel(0, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Inspiration];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(3M),
        new ExtraDamageVar(1M),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(CountCardsExhaustedThisTurn),
        ModCardVars.Int(InspirationKeyword.AmountVar, 1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    private static decimal CountCardsExhaustedThisTurn(CardModel card, Creature? _)
    {
        return CombatManager.Instance.History.Entries
            .OfType<CardExhaustedEntry>()
            .Count(entry =>
                entry.HappenedThisTurn(card.CombatState)
                && entry.Actor == card.Owner.Creature);
    }

    protected override void OnUpgrade() => DynamicVars.ExtraDamage.UpgradeValueBy(1M);
}

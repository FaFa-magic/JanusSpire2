using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class LonelyNight() : JanusCardModel(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const decimal MaximumDamage = 999999999M;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0M),
        new ExtraDamageVar(1M),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(CalculateGeneratedAttackDamage)
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

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator == Owner && CombatState != null)
        {
            this.RequestVisualReload();
        }

        return Task.CompletedTask;
    }

    private static decimal CalculateGeneratedAttackDamage(CardModel source, Creature? target)
    {
        decimal total = 0M;

        foreach (CardGeneratedEntry entry in CombatManager.Instance.History.Entries.OfType<CardGeneratedEntry>())
        {
            if (entry.Creator != source.Owner ||
                entry.Card.Type != CardType.Attack ||
                entry.Card is JanusRecordMappingCard)
            {
                continue;
            }
            
            decimal attackValue = entry.Card is LonelyNight
                ? ApplyEnchantment(entry.Card, total, ValueProp.Move)
                : GetAttackValue(entry.Card, target);

            total = Math.Min(MaximumDamage, total + Math.Max(0M, attackValue));
            if (total >= MaximumDamage)
            {
                break;
            }
        }

        return total;
    }

    private static decimal GetAttackValue(CardModel card, Creature? target)
    {
        if (card.DynamicVars.TryGetValue(DamageVar.defaultName, out DynamicVar? damageVar) &&
            damageVar is DamageVar damage)
        {
            return ApplyEnchantment(card, damage.BaseValue, damage.Props);
        }

        if (card.DynamicVars.TryGetValue(CalculatedDamageVar.defaultName, out DynamicVar? calculatedVar) &&
            calculatedVar is CalculatedDamageVar calculatedDamage)
        {
            return ApplyEnchantment(card, calculatedDamage.Calculate(target), calculatedDamage.Props);
        }

        if (card.DynamicVars.TryGetValue(OstyDamageVar.defaultName, out DynamicVar? ostyVar) &&
            ostyVar is OstyDamageVar ostyDamage)
        {
            return ApplyEnchantment(card, ostyDamage.BaseValue, ostyDamage.Props);
        }

        // A small number of cards use a named DamageVar instead of the canonical key.
        DamageVar? namedDamage = card.DynamicVars.Values.OfType<DamageVar>().FirstOrDefault();
        return namedDamage == null
            ? 0M
            : ApplyEnchantment(card, namedDamage.BaseValue, namedDamage.Props);
    }

    private static decimal ApplyEnchantment(CardModel card, decimal damage, ValueProp props)
    {
        if (card.Enchantment is not { } enchantment)
        {
            return damage;
        }

        damage += enchantment.EnchantDamageAdditive(damage, props);
        damage *= enchantment.EnchantDamageMultiplicative(damage, props);
        return Math.Max(0M, damage);
    }
    
    protected override void OnUpgrade() => base.EnergyCost.UpgradeBy(-1);
}

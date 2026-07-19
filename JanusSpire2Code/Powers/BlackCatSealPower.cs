using JanusSpire2.JanusSpire2Code.Interfaces;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interactions.RightClick;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class BlackCatSealPower : JanusPowerModel, IModRightClickablePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private const string FinalDmgKey = "JanusSpire2_BlackCat_FinalDmg";
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar(FinalDmgKey, 0m)
    ];

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        UpdateFinalDamage();
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal oldAmount, Creature? __, CardModel? cardSource)
    {
        if (power == this)
        {
            UpdateFinalDamage();
            InvokeDisplayAmountChanged();
        }

        return Task.CompletedTask;
    }

    private void UpdateFinalDamage()
    {
        if (Owner == null || !DynamicVars.ContainsKey(FinalDmgKey))
            return;

        int amount = this.Amount;
        decimal calculatedDmg = amount * 2m * (1m + 0.05m * amount);

        DynamicVars[FinalDmgKey].BaseValue = Math.Floor(calculatedDmg);

        InvokeDisplayAmountChanged();
    }
    
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (this.Owner?.CombatState == null || !props.HasFlag(ValueProp.Unpowered) || target != this.Owner)
            return 1M;
        
        return 1M + 0.05M * this.Amount;
    }
    
    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        var dmg = new DamageVar(Amount * 2, ValueProp.Unpowered);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, dmg, Owner);
        
        ArgumentNullException.ThrowIfNull(context.PlayerChoiceContext);
        await TriggerAfterBlackCatSealExplode(context.PlayerChoiceContext, new BlackCatSealExplodeContext
        {
            ChoiceContext = context.PlayerChoiceContext,
            Owner = Owner!
        });
        
        await PowerCmd.Remove(this);
    }
    
    public static async Task TriggerAfterBlackCatSealExplode(
        PlayerChoiceContext choiceContext,
        BlackCatSealExplodeContext context)
    {
        foreach (var hook in GetHooks<IAfterBlackCatSealExplode>(context.Owner))
        {
            await hook.AfterBlackCatSealExplode(choiceContext, context);
        }
    }

    private static IEnumerable<T> GetHooks<T>(Creature owner)
    {
        foreach (var card in owner.Player!.Piles.SelectMany(p => p.Cards))
        {
            if (card is T t)
                yield return t;
        }

        foreach (var power in owner.Powers)
        {
            if (power is T t)
                yield return t;
        }

        foreach (var relic in owner.Player.Relics)
        {
            if (relic is T t)
                yield return t;
        }
    }
}
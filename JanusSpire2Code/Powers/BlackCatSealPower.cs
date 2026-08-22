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
        ArgumentNullException.ThrowIfNull(context.PlayerChoiceContext);

        BreakAndRunPower? breakAndRun = Owner.GetPower<BreakAndRunPower>();
        if (breakAndRun != null)
        {
            await breakAndRun.BeforeBlackCatSealDamage(context.PlayerChoiceContext);
        }

        var dmg = new DamageVar(Amount * 2, ValueProp.Unpowered);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, dmg, Owner);
        
        await TriggerAfterBlackCatSealExplode(context.PlayerChoiceContext, new BlackCatSealExplodeContext
        {
            ChoiceContext = context.PlayerChoiceContext,
            Owner = Owner,
            Applier = Applier
        });
        
        await PowerCmd.Remove(this);
    }
    
    public static async Task TriggerAfterBlackCatSealExplode(
        PlayerChoiceContext choiceContext,
        BlackCatSealExplodeContext context)
    {
        foreach (var hook in GetHooks<IAfterBlackCatSealExplode>(context.Owner, context.Applier))
        {
            await hook.AfterBlackCatSealExplode(choiceContext, context);
        }
    }

    private static IReadOnlyList<T> GetHooks<T>(Creature owner, Creature? applier)
    {
        List<T> hooks = [];
        var player = applier?.Player;
        if (player?.PlayerCombatState != null)
        {
            foreach (var card in player.PlayerCombatState.AllCards)
            {
                if (card is T cardHook)
                    hooks.Add(cardHook);
            }

            foreach (var relic in player.Relics)
            {
                if (relic is T relicHook)
                    hooks.Add(relicHook);
            }
        }

        foreach (var power in owner.Powers)
        {
            if (power is T powerHook)
                hooks.Add(powerHook);
        }

        return hooks;
    }
}

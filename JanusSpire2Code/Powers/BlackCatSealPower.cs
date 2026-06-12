using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Powers;

[RegisterPower]
public sealed class BlackCatSealPower : ModPowerTemplate, IModRightClickablePower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://JanusSpire2/images/powers/big/BlackCatSealPower.png",
        BigIconPath: "res://JanusSpire2/images/powers/packed/BlackCatSealPower.png"
    );

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (this.Owner?.CombatState == null || !props.HasFlag(ValueProp.Unpowered))
            return 1M;
        
        return 1M + 0.05M * amount;
    }
    
    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        var dmg = new DamageVar(Amount * 2, ValueProp.Unpowered);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, dmg, Owner);
        
        await PowerCmd.Remove(this);
    }
}
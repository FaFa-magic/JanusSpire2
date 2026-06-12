using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Powers;

[RegisterPower]
public sealed class BattlePlanPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://JanusSpire2/images/powers/big/BlackCatSealPower.png",
        BigIconPath: "res://JanusSpire2/images/powers/packed/BlackCatSealPower.png"
    );
}
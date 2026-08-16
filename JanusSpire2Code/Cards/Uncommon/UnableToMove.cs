using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class UnableToMove() : JanusCardModel(-1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    public override bool GainsBlock => true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6M, ValueProp.Move | ValueProp.Unpowered)];
    
    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move | ValueProp.Unpowered, null);
    }
    
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3M);
}
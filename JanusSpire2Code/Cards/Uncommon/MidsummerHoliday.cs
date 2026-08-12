using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class MidsummerHoliday() : JanusCardModel(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Perk];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(8M, ValueProp.Move),
        new EnergyVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.BaseValue, base.Owner);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1M);
    }
}
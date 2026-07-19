using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class EscapeIntoSmoke() : JanusCardModel(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Counterattack];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(16M, ValueProp.Move),
        ModCardVars.Int("Smoke", 1),
        ModCardVars.Int("EscapeIntoSmoke", 1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<SmokePower>(choiceContext, base.Owner.Creature, DynamicVars["Smoke"].BaseValue, base.Owner.Creature, this);
        await PowerCmd.Apply<EscapeIntoSmokePower>(choiceContext, base.Owner.Creature, DynamicVars["EscapeIntoSmoke"].BaseValue, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(6M);
}
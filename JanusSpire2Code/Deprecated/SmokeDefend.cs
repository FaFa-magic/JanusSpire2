using JanusSpire2.JanusSpire2Code.Cards.Token;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Deprecated;

public sealed class SmokeDefend() : JanusTokenCardModel(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];

    public override int MaxUpgradeLevel => 999;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(5M, ValueProp.Move),
        ModCardVars.Int("Smoke", 1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }
    
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3M);
}
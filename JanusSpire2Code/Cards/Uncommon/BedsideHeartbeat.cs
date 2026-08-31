using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class BedsideHeartbeat() : JanusRecordCardModel(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Collection, JanusKeywords.Record, JanusKeywords.Recollection];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5M, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }
    
    public override async Task BeforeCombatStart()
    {
        if (Pile?.Type == MainFile.Diary)
        {
            await EnableTake();
        }
    }
    
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2M);
}

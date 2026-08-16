using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Common;

public sealed class Prayer() : JanusCardModel(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Record];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4M, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }
    
    protected override CardLocation GetResultLocationForCardPlay()
    {
        CardLocation resultLocationForCardPlay = base.GetResultLocationForCardPlay();
        if (resultLocationForCardPlay.pileType == PileType.Discard)
        {
            resultLocationForCardPlay.pileType = MainFile.Diary;
        }
        return resultLocationForCardPlay;
    }
    
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2M);
}
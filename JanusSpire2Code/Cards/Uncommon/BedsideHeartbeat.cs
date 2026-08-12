using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class BedsideHeartbeat() : JanusRecordCardModel(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6M, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }
    
    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(base.Owner.Creature))
        {
            return Task.CompletedTask;
        }
        if (base.Owner.PlayerCombatState != null && base.Owner.PlayerCombatState.TurnNumber == 1 && Pile?.Type == MainFile.Diary)
        {
            canTake = true;
        }
        
        return Task.CompletedTask;
    }
    
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3M);
}
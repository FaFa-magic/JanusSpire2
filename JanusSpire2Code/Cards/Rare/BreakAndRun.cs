using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class BreakAndRun() : JanusCardModel(2, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ModCardVars.Int("BlackCatSeal", 4),
        ModCardVars.Int("BreakAndRun", 1)
    ];
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<BlackCatSealPower>()
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState;
        ArgumentNullException.ThrowIfNull(combatState);

        foreach (var enemy in combatState.HittableEnemies)
        {
            await PowerCmd.Apply<BlackCatSealPower>(
                choiceContext,
                enemy,
                DynamicVars["BlackCatSeal"].BaseValue,
                Owner.Creature,
                this);
            await PowerCmd.Apply<BreakAndRunPower>(
                choiceContext,
                enemy,
                DynamicVars["BreakAndRun"].BaseValue,
                Owner.Creature,
                this);
        }
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["BlackCatSeal"].UpgradeValueBy(2M);
    }
}

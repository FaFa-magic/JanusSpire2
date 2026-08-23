using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class BlackCat : JanusRelicModel
{
    private const string BlackCatSealVar = "BlackCatSeal";

    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<BlackCatSealPower>(BlackCatSealVar, 4M)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<BlackCatSealPower>()
    ];

    public override async Task BeforeCombatStart()
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null || combatState.HittableEnemies.Count == 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<BlackCatSealPower>(
            new ThrowingPlayerChoiceContext(),
            combatState.HittableEnemies,
            DynamicVars[BlackCatSealVar].BaseValue,
            Owner.Creature,
            null);
    }
}

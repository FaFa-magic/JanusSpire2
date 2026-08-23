using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class CalicoCat : JanusRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(3M, ValueProp.Unpowered)
    ];

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        ICombatState? combatState = card.CombatState;
        if (creator != Owner || Owner.Creature.IsDead || combatState == null)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            combatState.HittableEnemies,
            DynamicVars.Damage,
            Owner.Creature);
    }
}

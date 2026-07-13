using JanusSpire2.JanusSpire2Code.Patches;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class ShiningCrown : JanusRelicModel, ISkipPlayerFlushRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ModCardVars.Int("Smoke", 2)
    ];
    
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(base.Owner.Creature) && base.Owner.PlayerCombatState?.TurnNumber <= 1)
        {
            await PowerCmd.Apply<SmokePower>(choiceContext, base.Owner.Creature, DynamicVars["Smoke"].BaseValue, base.Owner.Creature, null);
        }
    }
}
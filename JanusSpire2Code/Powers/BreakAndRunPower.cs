using MegaCrit.Sts2.Core.Entities.Powers;

using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class BreakAndRunPower : JanusPowerModel
{
    private int _lastTriggeredTurn;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    [SavedProperty]
    public int LastTriggeredTurn
    {
        get => _lastTriggeredTurn;
        set
        {
            AssertMutable();
            _lastTriggeredTurn = value;
        }
    }

    public async Task BeforeBlackCatSealDamage(PlayerChoiceContext choiceContext)
    {
        int currentTurn = Applier?.Player?.PlayerCombatState?.TurnNumber ?? CombatState.RoundNumber;
        if (LastTriggeredTurn == currentTurn ||
            Owner.IsDead ||
            CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        // Consume this enemy's once-per-turn trigger before applying powers so
        // nested hooks cannot cause the same instance to trigger twice.
        LastTriggeredTurn = currentTurn;

        Dictionary<PowerModel, int> debuffAmounts = Owner.Powers
            .Where(power => power.TypeForCurrentAmount == PowerType.Debuff)
            .Select(power => ((PowerModel)power.ClonePreservingMutability(), power.Amount))
            .ToDictionary(entry => entry.Item1, entry => entry.Amount);

        // Temporary stat powers also have an internally applied base power.
        // Normalize them the same way as the base game's Misery card so the
        // copied debuff has the same temporary behavior as the source.
        foreach ((PowerModel power, int amount) in debuffAmounts.ToArray())
        {
            if (power is not ITemporaryPower temporaryPower)
            {
                continue;
            }

            PowerModel? internallyAppliedPower = debuffAmounts.Keys
                .FirstOrDefault(candidate => candidate.Id == temporaryPower.InternallyAppliedPower.Id);
            if (internallyAppliedPower != null)
            {
                debuffAmounts[internallyAppliedPower] += amount;
            }
        }

        Creature[] otherEnemies = CombatState.Enemies
            .Where(enemy => enemy != Owner && enemy.IsAlive && enemy.CanReceivePowers)
            .ToArray();
        if (otherEnemies.Length == 0)
        {
            return;
        }

        Flash();
        int multiplier = Amount;
        foreach (Creature enemy in otherEnemies)
        {
            foreach ((PowerModel sourcePower, int sourceAmount) in debuffAmounts)
            {
                int amountToGive = (int)(sourceAmount * 0.5m * multiplier);
                if (amountToGive == 0)
                {
                    continue;
                }

                PowerModel? existingPower = PowerCmd.FindExistingInstanceForStacking(
                    sourcePower,
                    enemy,
                    sourcePower.Applier);
                if (existingPower != null)
                {
                    await PowerCmd.ModifyAmount(
                        choiceContext,
                        existingPower,
                        amountToGive,
                        sourcePower.Applier,
                        null);
                    continue;
                }

                PowerModel copiedPower = (PowerModel)sourcePower.ClonePreservingMutability();
                if (copiedPower is BreakAndRunPower copiedBreakAndRun)
                {
                    copiedBreakAndRun.LastTriggeredTurn = 0;
                }

                await PowerCmd.Apply(
                    choiceContext,
                    copiedPower,
                    enemy,
                    amountToGive,
                    sourcePower.Applier,
                    null);
            }
        }
    }
}

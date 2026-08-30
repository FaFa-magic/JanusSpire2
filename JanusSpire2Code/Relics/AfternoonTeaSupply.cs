using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class AfternoonTeaSupply : JanusRelicModel
{
    private const string StrengthVar = "StrengthPower";

    private PotionModel? _temporaryPotion;
    private int _temporaryPotionSlot = -1;
    private string _temporaryPotionId = string.Empty;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    [SavedProperty]
    public int TemporaryPotionSlot
    {
        get => _temporaryPotionSlot;
        private set
        {
            AssertMutable();
            _temporaryPotionSlot = value;
        }
    }

    [SavedProperty]
    public string TemporaryPotionId
    {
        get => _temporaryPotionId;
        private set
        {
            AssertMutable();
            _temporaryPotionId = value;
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(StrengthVar, 1M)
    ];

    public override async Task BeforeCombatStart()
    {
        // A potion can remain in the run inventory if a combat was interrupted after it was
        // generated. Clear that saved temporary potion before starting the new combat.
        await DiscardTemporaryPotion();

        Flash();
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            -DynamicVars[StrengthVar].BaseValue,
            Owner.Creature,
            null);
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature) || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        // Normally the preceding turn-end hook already did this. Keeping the cleanup here makes
        // extra turns and interrupted turn transitions deterministic as well.
        await DiscardTemporaryPotion();
        if (!Owner.HasOpenPotionSlots)
        {
            return;
        }

        PotionModel potion = PotionFactory.CreateRandomPotionInCombat(
            Owner,
            Owner.RunState.Rng.CombatPotionGeneration).ToMutable();
        PotionProcureResult result = await PotionCmd.TryToProcure(potion, Owner);
        if (!result.success)
        {
            return;
        }

        TrackTemporaryPotion(potion);
        Flash();
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner.Creature))
        {
            await DiscardTemporaryPotion();
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await DiscardTemporaryPotion();
    }

    public override Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (ReferenceEquals(potion, _temporaryPotion))
        {
            ClearTemporaryPotion();
        }

        return Task.CompletedTask;
    }

    public override Task AfterPotionDiscarded(PotionModel potion)
    {
        if (ReferenceEquals(potion, _temporaryPotion))
        {
            ClearTemporaryPotion();
        }

        return Task.CompletedTask;
    }

    private void TrackTemporaryPotion(PotionModel potion)
    {
        _temporaryPotion = potion;
        TemporaryPotionSlot = FindPotionSlot(potion);
        TemporaryPotionId = potion.Id.Entry;
    }

    private PotionModel? ResolveTemporaryPotion()
    {
        if (_temporaryPotion != null)
        {
            if (!_temporaryPotion.HasBeenRemovedFromState && Owner.Potions.Contains(_temporaryPotion))
            {
                return _temporaryPotion;
            }

            ClearTemporaryPotion();
            return null;
        }

        if (TemporaryPotionSlot < 0 ||
            TemporaryPotionSlot >= Owner.PotionSlots.Count ||
            string.IsNullOrEmpty(TemporaryPotionId))
        {
            ClearTemporaryPotion();
            return null;
        }

        PotionModel? savedPotion = Owner.PotionSlots[TemporaryPotionSlot];
        if (savedPotion == null || savedPotion.Id.Entry != TemporaryPotionId)
        {
            ClearTemporaryPotion();
            return null;
        }

        _temporaryPotion = savedPotion;
        return savedPotion;
    }

    private async Task DiscardTemporaryPotion()
    {
        PotionModel? potion = ResolveTemporaryPotion();
        if (potion == null)
        {
            return;
        }

        await PotionCmd.Discard(potion);
        ClearTemporaryPotion();
    }

    private int FindPotionSlot(PotionModel potion)
    {
        for (int i = 0; i < Owner.PotionSlots.Count; i++)
        {
            if (ReferenceEquals(Owner.PotionSlots[i], potion))
            {
                return i;
            }
        }

        return -1;
    }

    private void ClearTemporaryPotion()
    {
        _temporaryPotion = null;
        TemporaryPotionSlot = -1;
        TemporaryPotionId = string.Empty;
    }
}

using JanusSpire2.JanusSpire2Code.Cards.Uncommon;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Networking.ManagedActions;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class BlackCatSealPower : JanusPowerModel
{
    private static readonly RitsuLibManagedNetActionDescriptor<RightClickPayload> RightClickDescriptor = new(
        MainFile.ModId,
        "black_cat_seal_explode_by_combat_id",
        SerializeRightClickPayload,
        DeserializeRightClickPayload,
        ExecuteManagedRightClick,
        GameActionType.CombatPlayPhaseOnly);

    private static readonly BlackCatSealRightClickHandler RightClickHandler = new();
    private static int _rightClickRegistered;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private const string FinalDmgKey = "JanusSpire2_BlackCat_FinalDmg";

    internal static void RegisterSynchronizedRightClick()
    {
        if (Interlocked.Exchange(ref _rightClickRegistered, 1) != 0)
        {
            return;
        }

        RitsuLibManagedNetActions.Register(RightClickDescriptor);
        ModRightClickRegistry.Register(RightClickHandler);
    }
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar(FinalDmgKey, 0m)
    ];

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        UpdateFinalDamage();
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal oldAmount, Creature? __, CardModel? cardSource)
    {
        if (power == this)
        {
            UpdateFinalDamage();
            InvokeDisplayAmountChanged();
        }

        return Task.CompletedTask;
    }

    private void UpdateFinalDamage()
    {
        if (Owner == null || !DynamicVars.ContainsKey(FinalDmgKey))
            return;

        int amount = this.Amount;
        decimal calculatedDmg = amount * 2m * (1m + 0.02m * amount);

        DynamicVars[FinalDmgKey].BaseValue = Math.Floor(calculatedDmg);

        InvokeDisplayAmountChanged();
    }
    
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (this.Owner?.CombatState == null || !props.HasFlag(ValueProp.Unpowered) || target != this.Owner)
            return 1M;
        
        return 1M + 0.02M * this.Amount;
    }
    
    private bool CanExecuteRightClick()
    {
        return Owner.Powers.Contains(this) &&
               Owner.CombatId.HasValue &&
               !Owner.IsDead &&
               CombatManager.Instance.IsInProgress &&
               !CombatManager.Instance.IsOverOrEnding;
    }

    internal async Task<bool> Bloom(PlayerChoiceContext choiceContext)
    {
        if (!CanExecuteRightClick())
        {
            return false;
        }

        BreakAndRunPower? breakAndRun = Owner.GetPower<BreakAndRunPower>();
        if (breakAndRun != null)
        {
            await breakAndRun.BeforeBlackCatSealDamage(choiceContext);
        }

        var dmg = new DamageVar(Amount * 2, ValueProp.Unpowered);
        await CreatureCmd.Damage(choiceContext, Owner, dmg, Owner);

        await PowerCmd.Remove(this);
        ICombatState? combatState = Owner.CombatState;
        if (combatState != null)
        {
            await LittleDevil.RecallAfterBlackCatSealBloom(combatState);
        }
        return true;
    }

    private static byte[] SerializeRightClickPayload(RightClickPayload payload)
    {
        var writer = new PacketWriter { WarnOnGrow = false };
        writer.WriteUInt(payload.OwnerCombatId);
        writer.ZeroByteRemainder();
        return [.. writer.Buffer.AsSpan(0, writer.BytePosition)];
    }

    private static RightClickPayload DeserializeRightClickPayload(ReadOnlySpan<byte> bytes)
    {
        var reader = new PacketReader();
        reader.Reset(bytes.ToArray());
        return new(reader.ReadUInt());
    }

    private static async Task ExecuteManagedRightClick(
        RitsuLibManagedNetActionContext<RightClickPayload> context)
    {
        Creature? owner = context.Player.Creature.CombatState?
            .GetCreature(context.Message.OwnerCombatId);
        BlackCatSealPower? power = owner?.GetPower<BlackCatSealPower>();
        if (power == null || !power.CanExecuteRightClick())
        {
            return;
        }

        await power.Bloom(context.PlayerChoiceContext);
    }

    private readonly record struct RightClickPayload(uint OwnerCombatId);

    private sealed class BlackCatSealRightClickHandler : IModRightClickHandler
    {
        public int Priority => 100;

        public bool TryHandle(ModRightClickContext context)
        {
            if (context.Model is not BlackCatSealPower power ||
                context.Trigger.Source != ModRightClickSource.Power ||
                !power.CanExecuteRightClick() ||
                power.Owner.CombatId is not { } ownerCombatId)
            {
                return false;
            }

            return RitsuLibManagedNetActions.Request(
                RunManager.Instance,
                RightClickDescriptor,
                new(ownerCombatId),
                context.Player.NetId);
        }
    }
}

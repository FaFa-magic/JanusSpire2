using JanusSpire2.JanusSpire2Code.Cards.Uncommon;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Networking.ManagedActions;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class BlackCatSealPower : JanusPowerModel
{
    private static readonly RitsuLibManagedNetActionDescriptor<BloomRequest> BloomDescriptor = new(
        MainFile.ModId,
        "black_cat_seal_bloom_by_creature_click_v2",
        SerializeBloomRequest,
        DeserializeBloomRequest,
        ExecuteManagedBloom,
        GameActionType.CombatPlayPhaseOnly);

    private static int _bloomActionRegistered;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private const string FinalDmgKey = "JanusSpire2_BlackCat_FinalDmg";

    internal static void RegisterSynchronizedBloom()
    {
        if (Interlocked.Exchange(ref _bloomActionRegistered, 1) != 0)
        {
            return;
        }

        RitsuLibManagedNetActions.Register(BloomDescriptor);
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
    
    private bool CanBloom()
    {
        return Owner.Powers.Contains(this) &&
               Owner.CombatId.HasValue &&
               Owner.IsAlive &&
               CombatManager.Instance.IsInProgress &&
               !CombatManager.Instance.IsOverOrEnding;
    }

    private bool CanExecuteManualBloom(Player requester, int requestedTurnNumber)
    {
        ICombatState? combatState = Owner.CombatState;
        PlayerCombatState? playerCombatState = requester.PlayerCombatState;
        return CanBloom() &&
               combatState != null &&
               playerCombatState != null &&
               requester.Creature.IsAlive &&
               ReferenceEquals(requester.Creature.CombatState, combatState) &&
               combatState.CurrentSide == CombatSide.Player &&
               playerCombatState.Phase == PlayerTurnPhase.Play &&
               playerCombatState.TurnNumber == requestedTurnNumber &&
               !CombatManager.Instance.IsPlayerReadyToEndTurn(requester) &&
               RunManager.Instance.ActionQueueSynchronizer.CombatState ==
               ActionSynchronizerCombatState.PlayPhase;
    }

    internal static bool TryRequestManualBloom(Player requester, Creature target)
    {
        PlayerCombatState? playerCombatState = requester.PlayerCombatState;
        BlackCatSealPower? power = target.GetPower<BlackCatSealPower>();
        if (playerCombatState == null ||
            power == null ||
            CombatManager.Instance.PlayerActionsDisabled ||
            !power.CanExecuteManualBloom(requester, playerCombatState.TurnNumber) ||
            target.CombatId is not { } targetCombatId)
        {
            return false;
        }

        return RitsuLibManagedNetActions.Request(
            RunManager.Instance,
            BloomDescriptor,
            new(targetCombatId, playerCombatState.TurnNumber),
            requester.NetId);
    }

    internal async Task<bool> Bloom(PlayerChoiceContext choiceContext)
    {
        if (!CanBloom())
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

    private static byte[] SerializeBloomRequest(BloomRequest request)
    {
        var writer = new PacketWriter { WarnOnGrow = false };
        writer.WriteUInt(request.TargetCombatId);
        writer.WriteInt(request.RequesterTurnNumber);
        writer.ZeroByteRemainder();
        return [.. writer.Buffer.AsSpan(0, writer.BytePosition)];
    }

    private static BloomRequest DeserializeBloomRequest(ReadOnlySpan<byte> bytes)
    {
        var reader = new PacketReader();
        reader.Reset(bytes.ToArray());
        return new(reader.ReadUInt(), reader.ReadInt());
    }

    private static async Task ExecuteManagedBloom(
        RitsuLibManagedNetActionContext<BloomRequest> context)
    {
        Creature? target = context.Player.Creature.CombatState?
            .GetCreature(context.Message.TargetCombatId);
        BlackCatSealPower? power = target?.GetPower<BlackCatSealPower>();
        if (power == null ||
            !power.CanExecuteManualBloom(context.Player, context.Message.RequesterTurnNumber))
        {
            return;
        }

        await power.Bloom(context.PlayerChoiceContext);
    }

    private readonly record struct BloomRequest(uint TargetCombatId, int RequesterTurnNumber);
}

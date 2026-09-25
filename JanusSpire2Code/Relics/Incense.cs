using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Networking.ManagedActions;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class Incense : JanusRelicModel
{
    private static readonly RitsuLibManagedNetActionDescriptor<UseRequest> UseDescriptor = new(
        MainFile.ModId,
        "incense_use_v1",
        SerializeUseRequest,
        DeserializeUseRequest,
        ExecuteManagedUse,
        GameActionType.CombatPlayPhaseOnly);

    private static readonly IncenseRightClickHandler RightClickHandler = new();
    private static int _rightClickRegistered;
    private bool _usedThisCombat;

    internal static void RegisterSynchronizedRightClick()
    {
        if (Interlocked.Exchange(ref _rightClickRegistered, 1) != 0)
        {
            return;
        }

        RitsuLibManagedNetActions.Register(UseDescriptor);
        ModRightClickRegistry.Register(RightClickHandler);
    }

    public override RelicRarity Rarity => RelicRarity.Rare;

    [SavedProperty]
    public bool UsedThisCombat
    {
        get => _usedThisCombat;
        set
        {
            AssertMutable();
            _usedThisCombat = value;
            RefreshStatus();
        }
    }

    public override Task AfterObtained()
    {
        RefreshStatus();
        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        UsedThisCombat = false;
        return Task.CompletedTask;
    }

    private void RefreshStatus()
    {
        Status = CombatManager.Instance.IsInProgress && !UsedThisCombat
            ? RelicStatus.Active
            : RelicStatus.Normal;
    }

    private bool CanUse(Player player, int turnNumber)
    {
        PlayerCombatState? playerCombatState = player.PlayerCombatState;
        return ReferenceEquals(player, Owner) &&
               Owner.Relics.Contains(this) &&
               Owner.Creature.IsAlive &&
               playerCombatState is { Phase: PlayerTurnPhase.Play } &&
               playerCombatState.TurnNumber == turnNumber &&
               CombatManager.Instance.IsPartOfPlayerTurn(player) &&
               !CombatManager.Instance.IsPlayerReadyToEndTurn(player) &&
               RunManager.Instance.ActionQueueSynchronizer.CombatState ==
                   ActionSynchronizerCombatState.PlayPhase &&
               CombatManager.Instance.IsInProgress &&
               !CombatManager.Instance.IsOverOrEnding &&
               Owner.Creature.CombatState is not null &&
               !UsedThisCombat &&
               PileType.Hand.GetPile(Owner).Cards.Count > 0;
    }

    private async Task Use(GameActionPlayerChoiceContext choiceContext)
    {
        List<CardModel> handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        int cardsToDraw = handCards.Count;

        UsedThisCombat = true;
        Flash();

        foreach (CardModel card in handCards)
        {
            await CardPileCmd.Add(card, PileType.Draw);
        }

        await CardPileCmd.Shuffle(choiceContext, Owner);
        await CardPileCmd.Draw(choiceContext, cardsToDraw, Owner);
    }

    private static byte[] SerializeUseRequest(UseRequest request)
    {
        var writer = new PacketWriter { WarnOnGrow = false };
        writer.WriteInt(request.RelicIndex);
        writer.WriteInt(request.TurnNumber);
        writer.ZeroByteRemainder();
        return [.. writer.Buffer.AsSpan(0, writer.BytePosition)];
    }

    private static UseRequest DeserializeUseRequest(ReadOnlySpan<byte> bytes)
    {
        var reader = new PacketReader();
        reader.Reset(bytes.ToArray());
        return new(reader.ReadInt(), reader.ReadInt());
    }

    private static async Task ExecuteManagedUse(RitsuLibManagedNetActionContext<UseRequest> context)
    {
        // Relic inventory order is synchronized; a runtime model token can differ between peers.
        int relicIndex = context.Message.RelicIndex;
        if (relicIndex < 0 || relicIndex >= context.Player.Relics.Count ||
            context.Player.Relics[relicIndex] is not Incense incense ||
            !incense.CanUse(context.Player, context.Message.TurnNumber))
        {
            return;
        }

        await incense.Use(context.PlayerChoiceContext);
    }

    private readonly record struct UseRequest(int RelicIndex, int TurnNumber);

    private sealed class IncenseRightClickHandler : IModRightClickHandler
    {
        public int Priority => 100;

        public bool TryHandle(ModRightClickContext context)
        {
            if (context.Trigger.Source != ModRightClickSource.Relic ||
                context.Model is not Incense incense ||
                !LocalContext.IsMe(context.Player) ||
                context.Player.PlayerCombatState is not { } playerCombatState ||
                !incense.CanUse(context.Player, playerCombatState.TurnNumber) ||
                CombatManager.Instance.PlayerActionsDisabled)
            {
                return false;
            }

            for (int index = 0; index < context.Player.Relics.Count; index++)
            {
                if (ReferenceEquals(context.Player.Relics[index], incense))
                {
                    return RitsuLibManagedNetActions.Request(
                        RunManager.Instance,
                        UseDescriptor,
                        new(index, playerCombatState.TurnNumber),
                        context.Player.NetId);
                }
            }

            return false;
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        UsedThisCombat = false;
        return Task.CompletedTask;
    }
}

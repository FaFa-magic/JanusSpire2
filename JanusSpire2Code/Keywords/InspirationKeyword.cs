using JanusSpire2.JanusSpire2Code.Nodes;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Networking.ManagedActions;

namespace JanusSpire2.JanusSpire2Code.Keywords;

public static class InspirationKeyword
{
    public const string AmountVar = "Inspiration";

    private static readonly RitsuLibManagedNetActionDescriptor<RightClickPayload> RightClickDescriptor = new(
        MainFile.ModId,
        "inspiration_take_from_diary_v2",
        SerializeRightClickPayload,
        DeserializeRightClickPayload,
        ExecuteManagedRightClick,
        GameActionType.CombatPlayPhaseOnly);

    private static readonly InspirationRightClickHandler RightClickHandler = new();
    private static int _rightClickRegistered;

    internal static void RegisterSynchronizedRightClick()
    {
        if (Interlocked.Exchange(ref _rightClickRegistered, 1) != 0)
        {
            return;
        }

        RitsuLibManagedNetActions.Register(RightClickDescriptor);
        ModRightClickRegistry.Register(RightClickHandler);
    }

    private static bool IsSupportedRightClickPile(PileType pileType)
    {
        return pileType is PileType.Draw or PileType.Discard or PileType.Exhaust ||
               pileType == MainFile.Diary;
    }

    private static bool TryGetAmount(CardModel card, out int amount)
    {
        amount = 0;
        if (!card.Keywords.Contains(JanusKeywords.Inspiration) ||
            !card.DynamicVars.TryGetValue(AmountVar, out var amountVar))
        {
            return false;
        }

        amount = amountVar.IntValue;
        return amount >= 0;
    }

    internal static bool ShouldGlow(CardModel card)
    {
        if (card.IsCanonical ||
            card.Owner?.PlayerCombatState == null ||
            card.Pile is not { } sourcePile ||
            sourcePile.Type == PileType.Hand ||
            !IsSupportedRightClickPile(sourcePile.Type) ||
            !TryGetAmount(card, out int amount))
        {
            return false;
        }

        CardPile? diaryPile = card.Owner.PlayerCombatState.AllPiles
            .FirstOrDefault(pile => pile.Type == MainFile.Diary);
        return diaryPile != null && diaryPile.Cards.Count >= amount;
    }

    private static bool CanExecuteRightClick(CardModel card, PileType expectedPile, out int amount)
    {
        amount = 0;
        if (card.Owner?.PlayerCombatState == null ||
            card.Pile?.Type != expectedPile ||
            !IsSupportedRightClickPile(expectedPile) ||
            !TryGetAmount(card, out amount))
        {
            return false;
        }

        CardPile hand = PileType.Hand.GetPile(card.Owner);
        CardPile? diaryPile = card.Owner.PlayerCombatState.AllPiles
            .FirstOrDefault(pile => pile.Type == MainFile.Diary);
        return hand.Cards.Count < CardPile.MaxCardsInHand &&
               diaryPile != null &&
               diaryPile.Cards.Count >= amount;
    }

    private static bool CanAct(Player player, int turnNumber)
    {
        PlayerCombatState? combatState = player.PlayerCombatState;
        return CombatManager.Instance.IsInProgress &&
               !CombatManager.Instance.IsOverOrEnding &&
               player.Creature.IsAlive &&
               combatState is { Phase: PlayerTurnPhase.Play } &&
               combatState.TurnNumber == turnNumber &&
               CombatManager.Instance.IsPartOfPlayerTurn(player) &&
               !CombatManager.Instance.IsPlayerReadyToEndTurn(player) &&
               RunManager.Instance.ActionQueueSynchronizer.CombatState ==
               ActionSynchronizerCombatState.PlayPhase;
    }

    private static async Task ExecuteRightClick(
        GameActionPlayerChoiceContext choiceContext,
        Player actionPlayer,
        CardModel card,
        PileType expectedPile,
        int turnNumber)
    {
        if (!CanAct(actionPlayer, turnNumber) ||
            !ReferenceEquals(actionPlayer, card.Owner) ||
            !CanExecuteRightClick(card, expectedPile, out int amount))
        {
            return;
        }

        CardPile diaryPile = MainFile.Diary.GetPile(card.Owner);
        if (amount > 0)
        {
            // A fixed Diary snapshot makes the official multiplayer choice synchronizer
            // transmit positional indexes instead of unstable presentation-card IDs.
            List<CardModel> diarySnapshot = diaryPile.Cards.ToList();
            List<CardModel> selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                diarySnapshot,
                card.Owner,
                new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, amount))).ToList();

            if (!CanAct(actionPlayer, turnNumber) ||
                !CanExecuteRightClick(card, expectedPile, out int currentAmount) ||
                currentAmount != amount ||
                selected.Count != amount ||
                selected.Distinct().Count() != amount ||
                selected.Any(selectedCard => !diaryPile.Cards.Contains(selectedCard)))
            {
                return;
            }

            foreach (CardModel selectedCard in selected)
            {
                await CardCmd.Exhaust(choiceContext, selectedCard);
            }
        }

        // Exhaust hooks can end combat or remove a card entirely. Do not restore a card
        // after combat cleanup; selecting the Inspiration card itself as payment is valid.
        if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsOverOrEnding ||
            card.Pile is not { IsCombatPile: true })
        {
            return;
        }

        await CardPileCmd.Add(card, PileType.Hand);
    }

    private static byte[] SerializeRightClickPayload(RightClickPayload payload)
    {
        var writer = new PacketWriter { WarnOnGrow = false };
        // WriteEnum reserves bits for vanilla values only and truncates custom pile IDs.
        writer.WriteInt((int)payload.SourcePile);
        writer.WriteUInt(payload.CombatCardId);
        writer.WriteInt(payload.TurnNumber);
        writer.ZeroByteRemainder();
        return [.. writer.Buffer.AsSpan(0, writer.BytePosition)];
    }

    private static RightClickPayload DeserializeRightClickPayload(ReadOnlySpan<byte> bytes)
    {
        var reader = new PacketReader();
        reader.Reset(bytes.ToArray());
        return new((PileType)reader.ReadInt(), reader.ReadUInt(), reader.ReadInt());
    }

    private static async Task ExecuteManagedRightClick(
        RitsuLibManagedNetActionContext<RightClickPayload> context)
    {
        // Resolve the exact card, even if another queued action reordered its pile.
        if (!NetCombatCardDb.Instance.TryGetCard(context.Message.CombatCardId, out CardModel? card) ||
            card == null)
        {
            return;
        }

        await ExecuteRightClick(
            context.PlayerChoiceContext,
            context.Player,
            card,
            context.Message.SourcePile,
            context.Message.TurnNumber);
    }

    private readonly record struct RightClickPayload(
        PileType SourcePile,
        uint CombatCardId,
        int TurnNumber);

    private sealed class InspirationRightClickHandler : IModRightClickHandler
    {
        public int Priority => 100;

        public bool TryHandle(ModRightClickContext context)
        {
            if (!LocalContext.IsMe(context.Player) ||
                context.Model is not CardModel { IsMutable: true } card ||
                !ReferenceEquals(context.Player, card.Owner) ||
                context.Player.PlayerCombatState is not { } combatState ||
                !CanAct(context.Player, combatState.TurnNumber) ||
                CombatManager.Instance.PlayerActionsDisabled ||
                context.Trigger.Source != ModRightClickSource.CombatPileCard ||
                context.Trigger.ExpectedCardPile is not { } expectedPile ||
                !CanExecuteRightClick(card, expectedPile, out _))
            {
                return false;
            }

            if (!NetCombatCardDb.Instance.TryGetCardId(card, out uint combatCardId))
            {
                return false;
            }

            // Close only the pile being clicked, before queuing. Never close a choice
            // screen (or a remote player's UI) from the synchronized executor.
            if (NCapstoneContainer.Instance is { } capstone &&
                (capstone.CurrentCapstoneScreen switch
                {
                    NCardPileScreen screen => ReferenceEquals(screen.Pile, card.Pile),
                    NDiaryCardPileScreen screen => ReferenceEquals(screen.Pile, card.Pile),
                    _ => false
                }))
            {
                capstone.Close();
            }

            return RitsuLibManagedNetActions.Request(
                RunManager.Instance,
                RightClickDescriptor,
                new(expectedPile, combatCardId, combatState.TurnNumber),
                context.Player.NetId);
        }
    }
}

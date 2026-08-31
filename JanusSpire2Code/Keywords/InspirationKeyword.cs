using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
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
        "inspiration_take_from_diary",
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

    private static async Task ExecuteRightClick(
        GameActionPlayerChoiceContext choiceContext,
        Player actionPlayer,
        CardModel card,
        PileType expectedPile)
    {
        if (!ReferenceEquals(actionPlayer, card.Owner) ||
            !CanExecuteRightClick(card, expectedPile, out int amount))
        {
            return;
        }

        CardPile diaryPile = MainFile.Diary.GetPile(card.Owner);
        if (LocalContext.IsMe(actionPlayer) &&
            NCapstoneContainer.Instance is { InUse: true } capstone)
        {
            capstone.Close();
        }

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

            if (selected.Count != amount ||
                selected.Distinct().Count() != amount ||
                selected.Any(selectedCard => selectedCard.Pile?.Type != MainFile.Diary))
            {
                return;
            }

            foreach (CardModel selectedCard in selected)
            {
                await CardCmd.Exhaust(choiceContext, selectedCard);
            }
        }

        await CardPileCmd.Add(card, PileType.Hand);
    }

    private static byte[] SerializeRightClickPayload(RightClickPayload payload)
    {
        var writer = new PacketWriter { WarnOnGrow = false };
        writer.WriteEnum(payload.SourcePile);
        writer.WriteInt(payload.CardIndex);
        writer.ZeroByteRemainder();
        return [.. writer.Buffer.AsSpan(0, writer.BytePosition)];
    }

    private static RightClickPayload DeserializeRightClickPayload(ReadOnlySpan<byte> bytes)
    {
        var reader = new PacketReader();
        reader.Reset(bytes.ToArray());
        return new(reader.ReadEnum<PileType>(), reader.ReadInt());
    }

    private static async Task ExecuteManagedRightClick(
        RitsuLibManagedNetActionContext<RightClickPayload> context)
    {
        CardPile? sourcePile = context.Player.PlayerCombatState?.AllPiles
            .FirstOrDefault(pile => pile.Type == context.Message.SourcePile);
        if (sourcePile == null ||
            context.Message.CardIndex < 0 ||
            context.Message.CardIndex >= sourcePile.Cards.Count)
        {
            return;
        }

        CardModel card = sourcePile.Cards[context.Message.CardIndex];
        await ExecuteRightClick(
            context.PlayerChoiceContext,
            context.Player,
            card,
            context.Message.SourcePile);
    }

    private readonly record struct RightClickPayload(PileType SourcePile, int CardIndex);

    private sealed class InspirationRightClickHandler : IModRightClickHandler
    {
        public int Priority => 100;

        public bool TryHandle(ModRightClickContext context)
        {
            if (context.Model is not CardModel card ||
                !ReferenceEquals(context.Player, card.Owner) ||
                context.Trigger.Source != ModRightClickSource.CombatPileCard ||
                context.Trigger.ExpectedCardPile is not { } expectedPile ||
                !CanExecuteRightClick(card, expectedPile, out _))
            {
                return false;
            }

            int cardIndex = card.Pile!.Cards.ToList().IndexOf(card);
            if (cardIndex < 0)
            {
                return false;
            }

            return RitsuLibManagedNetActions.Request(
                RunManager.Instance,
                RightClickDescriptor,
                new(expectedPile, cardIndex),
                context.Player.NetId);
        }
    }
}

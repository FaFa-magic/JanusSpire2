using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Networking.ManagedActions;

namespace JanusSpire2.JanusSpire2Code.Cards;

internal static class StickerMergeAction
{
    private static readonly RitsuLibManagedNetActionDescriptor<MergePayload> Descriptor = new(
        MainFile.ModId,
        "merge_exhausted_stickers",
        Serialize,
        Deserialize,
        Execute,
        GameActionType.Combat);

    private static int _registered;

    internal static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 0)
        {
            RitsuLibManagedNetActions.Register(Descriptor);
        }
    }

    internal static bool Request(CardModel trigger, int mergeCount)
    {
        if (mergeCount <= 0 ||
            trigger.Pile?.Type != PileType.Exhaust ||
            !trigger.Keywords.Contains(JanusKeywords.Sticker) ||
            !trigger.IsUpgradable)
        {
            return false;
        }

        int matchingCount = PileType.Exhaust.GetPile(trigger.Owner).Cards.Count(card =>
            card.Id == trigger.Id &&
            card.CurrentUpgradeLevel == trigger.CurrentUpgradeLevel &&
            card.Keywords.Contains(JanusKeywords.Sticker) &&
            card.IsUpgradable);
        if (matchingCount < mergeCount)
        {
            return false;
        }

        Register();
        return RitsuLibManagedNetActions.Request(
            RunManager.Instance,
            Descriptor,
            new(trigger.Id, trigger.CurrentUpgradeLevel, mergeCount),
            trigger.Owner.NetId);
    }

    private static byte[] Serialize(MergePayload payload)
    {
        var writer = new PacketWriter { WarnOnGrow = false };
        writer.WriteFullModelId(payload.CardId);
        writer.WriteInt(payload.UpgradeLevel);
        writer.WriteInt(payload.MergeCount);
        writer.ZeroByteRemainder();
        return [.. writer.Buffer.AsSpan(0, writer.BytePosition)];
    }

    private static MergePayload Deserialize(ReadOnlySpan<byte> bytes)
    {
        var reader = new PacketReader();
        reader.Reset(bytes.ToArray());
        return new(reader.ReadFullModelId(), reader.ReadInt(), reader.ReadInt());
    }

    private static async Task Execute(RitsuLibManagedNetActionContext<MergePayload> context)
    {
        MergePayload payload = context.Message;
        ICombatState? combatState = context.Player.Creature.CombatState;
        if (combatState == null ||
            !CombatManager.Instance.IsInProgress ||
            CombatManager.Instance.IsOverOrEnding ||
            payload.MergeCount <= 0)
        {
            return;
        }

        List<CardModel> matchingStickers = PileType.Exhaust.GetPile(context.Player).Cards
            .Where(card =>
                card.Id == payload.CardId &&
                card.CurrentUpgradeLevel == payload.UpgradeLevel &&
                card.Keywords.Contains(JanusKeywords.Sticker) &&
                card.IsUpgradable)
            .Take(payload.MergeCount)
            .ToList();
        if (matchingStickers.Count < payload.MergeCount)
        {
            return;
        }

        CardModel canonical = ModelDb.GetById<CardModel>(payload.CardId);
        CardModel upgradedSticker = combatState.CreateCard(canonical, context.Player);
        for (int i = 0; i <= payload.UpgradeLevel; i++)
        {
            CardCmd.Upgrade(upgradedSticker, CardPreviewStyle.None);
        }

        await CardPileCmd.RemoveFromCombat(matchingStickers);
        CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(
            upgradedSticker,
            MainFile.Diary,
            context.Player);
        CardCmd.PreviewCardPileAdd(result, 0.2F);
    }

    private readonly record struct MergePayload(
        ModelId CardId,
        int UpgradeLevel,
        int MergeCount);
}

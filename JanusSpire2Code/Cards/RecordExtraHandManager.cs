using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace JanusSpire2.JanusSpire2Code.Cards;

/// <summary>
/// Keeps one stable presentation mapping for every Record card that has become takeable during
/// the combat. Active mappings use RitsuLib's extra hand; inactive mappings remain in a headless
/// combat pile so their multiplayer card IDs can be reused safely.
/// </summary>
internal static class RecordExtraHandManager
{
    internal const int MaxMappings = 99;

    internal static async Task SyncFor(JanusRecordCardModel card)
    {
        if (card.IsMutable && card.Owner?.PlayerCombatState != null && card.Owner.Creature.CombatState != null)
        {
            await SyncPlayer(card.Owner);
        }
    }

    internal static async Task SyncPlayer(Player player)
    {
        PlayerCombatState? playerCombatState = player.PlayerCombatState;
        ICombatState? combatState = player.Creature.CombatState;
        if (playerCombatState == null || combatState == null)
        {
            return;
        }

        CardPile? diary = FindPile(playerCombatState, MainFile.Diary);
        CardPile? extraHand = FindPile(playerCombatState, MainFile.RecordExtraHand);
        CardPile? storage = FindPile(playerCombatState, MainFile.RecordMappingStorage);
        if (diary == null || extraHand == null || storage == null)
        {
            return;
        }

        EnsureUniqueMappingKeys(playerCombatState);

        List<JanusRecordCardModel> allRecords = playerCombatState.AllCards
            .OfType<JanusRecordCardModel>()
            .ToList();
        Dictionary<int, JanusRecordCardModel> recordsByKey = allRecords
            .ToDictionary(card => card.RecordMappingKey);
        List<JanusRecordCardModel> eligibleOriginals = diary.Cards
            .OfType<JanusRecordCardModel>()
            .Where(card => card.CanTake)
            .Take(MaxMappings)
            .ToList();
        HashSet<int> eligibleKeys = eligibleOriginals
            .Select(card => card.RecordMappingKey)
            .ToHashSet();

        List<JanusRecordMappingCard> mappings = playerCombatState.AllPiles
            .SelectMany(pile => pile.Cards)
            .OfType<JanusRecordMappingCard>()
            .ToList();
        Dictionary<int, JanusRecordMappingCard> retainedMappings = [];

        foreach (JanusRecordMappingCard mapping in mappings)
        {
            if (!recordsByKey.TryGetValue(mapping.OriginalRecordMappingKey, out JanusRecordCardModel? original) ||
                retainedMappings.ContainsKey(original.RecordMappingKey))
            {
                mapping.Unbind();
                await MoveMapping(mapping, storage);
                continue;
            }

            mapping.Bind(original);
            retainedMappings[original.RecordMappingKey] = mapping;

            CardPile targetPile = eligibleKeys.Contains(original.RecordMappingKey)
                ? extraHand
                : storage;
            await MoveMapping(mapping, targetPile);
        }

        foreach (JanusRecordCardModel original in eligibleOriginals)
        {
            if (retainedMappings.ContainsKey(original.RecordMappingKey))
            {
                continue;
            }

            JanusRecordMappingCard mapping = combatState.CreateCard<JanusRecordMappingCard>(player);
            mapping.Bind(original);
            retainedMappings[original.RecordMappingKey] = mapping;

            // RitsuLib 0.5.17 creates and owns the holder through CardPileCmd's vanilla-hand
            // visual branch. AddInternal here would update state without creating a playable node.
            await CardPileCmd.Add(mapping, MainFile.RecordExtraHand);
        }
    }

    internal static JanusRecordCardModel? ResolveOriginal(JanusRecordMappingCard mapping)
    {
        if (mapping.Original is { } bound &&
            bound.RecordMappingKey == mapping.OriginalRecordMappingKey &&
            ReferenceEquals(bound.Owner, mapping.Owner))
        {
            return bound;
        }

        PlayerCombatState? state = mapping.Owner?.PlayerCombatState;
        if (state == null)
        {
            return null;
        }

        JanusRecordCardModel? original = state.AllCards
            .OfType<JanusRecordCardModel>()
            .FirstOrDefault(card =>
                card.RecordMappingKey == mapping.OriginalRecordMappingKey &&
                ReferenceEquals(card.Owner, mapping.Owner));
        if (original != null)
        {
            mapping.Bind(original);
        }

        return original;
    }

    /// <summary>
    /// Called only after RitsuLib has released the selected holder into the official play queue.
    /// Moving the mapping directly at this point closes RitsuLib's pending origin without touching
    /// the queued visual, which has already been rebound to the real card.
    /// </summary>
    internal static void ParkMappingForPlay(JanusRecordMappingCard mapping)
    {
        CardPile? source = mapping.Pile;
        CardPile? storage = mapping.Owner?.PlayerCombatState == null
            ? null
            : FindPile(mapping.Owner.PlayerCombatState, MainFile.RecordMappingStorage);
        if (source == null || storage == null || ReferenceEquals(source, storage))
        {
            return;
        }

        source.RemoveInternal(mapping);
        storage.AddInternal(mapping);
    }

    private static async Task MoveMapping(JanusRecordMappingCard mapping, CardPile destination)
    {
        if (ReferenceEquals(mapping.Pile, destination))
        {
            return;
        }

        // A synchronized state change can make a projection unavailable while its owner is still
        // choosing a target locally. Cancel that UI-only targeting first; RitsuLib restores the
        // same holder, after which every peer performs the identical backend pile move below.
        if (mapping.Pile?.Type == MainFile.RecordExtraHand &&
            destination.Type == MainFile.RecordMappingStorage)
        {
            NPlayerHand.Instance?.TryCancelCardPlay(mapping);
        }

        await CardPileCmd.Add(mapping, destination.Type);
    }

    private static CardPile? FindPile(PlayerCombatState state, PileType pileType)
    {
        return state.AllPiles.FirstOrDefault(pile => pile.Type == pileType);
    }

    private static void EnsureUniqueMappingKeys(PlayerCombatState state)
    {
        List<JanusRecordCardModel> records = state.AllCards
            .OfType<JanusRecordCardModel>()
            .ToList();
        int nextKey = records
            .Select(card => card.RecordMappingKey)
            .DefaultIfEmpty(0)
            .Max() + 1;
        HashSet<int> usedKeys = [];

        foreach (JanusRecordCardModel record in records)
        {
            if (record.RecordMappingKey > 0 && usedKeys.Add(record.RecordMappingKey))
            {
                continue;
            }

            record.RecordMappingKey = nextKey++;
            usedKeys.Add(record.RecordMappingKey);
        }
    }
}

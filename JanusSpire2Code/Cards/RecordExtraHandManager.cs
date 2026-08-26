using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using JanusSpire2.JanusSpire2Code.Patches;

namespace JanusSpire2.JanusSpire2Code.Cards;

internal static class RecordExtraHandManager
{
    internal const int MaxMappings = 99;

    internal static void SyncFor(JanusRecordCardModel card)
    {
        if (!card.IsMutable || card.CombatState == null)
        {
            return;
        }

        SyncPlayer(card.Owner);
    }

    internal static void SyncPlayer(Player player)
    {
        PlayerCombatState? playerCombatState = player.PlayerCombatState;
        ICombatState? combatState = player.Creature.CombatState;
        if (playerCombatState == null || combatState == null)
        {
            return;
        }

        CardPile? diary = playerCombatState.AllPiles.FirstOrDefault(pile => pile.Type == MainFile.Diary);
        CardPile? extraHand = playerCombatState.AllPiles.FirstOrDefault(pile => pile.Type == MainFile.RecordExtraHand);
        if (diary == null || extraHand == null)
        {
            return;
        }

        EnsureUniqueMappingKeys(playerCombatState);

        List<JanusRecordCardModel> desiredOriginals = diary.Cards
            .OfType<JanusRecordCardModel>()
            .Where(card => card.CanTake && !RecordMappingPlayRedirect.IsQueued(card))
            .Take(MaxMappings)
            .ToList();
        Dictionary<int, JanusRecordCardModel> desiredByKey = desiredOriginals
            .ToDictionary(card => card.RecordMappingKey);

        List<JanusRecordMappingCard> existingMappings = playerCombatState.AllPiles
            .SelectMany(pile => pile.Cards)
            .OfType<JanusRecordMappingCard>()
            .ToList();
        HashSet<int> retainedKeys = [];

        foreach (JanusRecordMappingCard mapping in existingMappings)
        {
            // RitsuLib temporarily puts a selected extra-hand card in Hand while targeting.
            // Leave that mapping alone until targeting is canceled or its action executes.
            if (mapping.Pile?.Type == PileType.Hand)
            {
                if (desiredByKey.TryGetValue(
                        mapping.OriginalRecordMappingKey,
                        out JanusRecordCardModel? targetedOriginal))
                {
                    mapping.Bind(targetedOriginal);
                    retainedKeys.Add(targetedOriginal.RecordMappingKey);
                }

                continue;
            }

            if (mapping.Pile?.Type == MainFile.RecordExtraHand &&
                desiredByKey.TryGetValue(mapping.OriginalRecordMappingKey, out JanusRecordCardModel? original) &&
                retainedKeys.Add(original.RecordMappingKey))
            {
                mapping.Bind(original);
                continue;
            }

            RemoveMapping(combatState, mapping);
        }

        int liveMappingCount = existingMappings.Count(mapping =>
            mapping.Pile?.Type == PileType.Hand || mapping.Pile?.Type == MainFile.RecordExtraHand);
        foreach (JanusRecordCardModel original in desiredOriginals)
        {
            if (!retainedKeys.Add(original.RecordMappingKey))
            {
                continue;
            }

            if (liveMappingCount >= MaxMappings)
            {
                break;
            }

            JanusRecordMappingCard mapping = combatState.CreateCard<JanusRecordMappingCard>(player);
            mapping.Bind(original);
            extraHand.AddInternal(mapping);
            liveMappingCount++;
        }
    }

    internal static void RemoveMappingFor(JanusRecordCardModel original)
    {
        ICombatState? combatState = original.Owner.Creature.CombatState;
        PlayerCombatState? playerCombatState = original.Owner.PlayerCombatState;
        if (combatState == null || playerCombatState == null)
        {
            return;
        }

        foreach (JanusRecordMappingCard mapping in playerCombatState.AllPiles
                     .SelectMany(pile => pile.Cards)
                     .OfType<JanusRecordMappingCard>()
                     .Where(mapping => ReferenceEquals(mapping.Original, original) ||
                                       mapping.OriginalRecordMappingKey == original.RecordMappingKey)
                     .ToArray())
        {
            RemoveMapping(combatState, mapping);
        }
    }

    private static void EnsureUniqueMappingKeys(PlayerCombatState state)
    {
        List<JanusRecordCardModel> records = state.AllCards
            .OfType<JanusRecordCardModel>()
            .ToList();
        int nextKey = records.Select(card => card.RecordMappingKey).DefaultIfEmpty(0).Max() + 1;
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

    private static void RemoveMapping(ICombatState combatState, JanusRecordMappingCard mapping)
    {
        mapping.Pile?.RemoveInternal(mapping);
        if (combatState.ContainsCard(mapping))
        {
            combatState.RemoveCard(mapping);
        }
    }
}

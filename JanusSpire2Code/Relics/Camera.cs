using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Content;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class Camera : JanusRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<string> RegisteredKeywordIds =>
    [
        ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, nameof(JanusKeywords.Collection))
    ];

    public override bool TryModifyCardRewardOptions(
        Player player,
        List<CardCreationResult> cardRewards,
        CardCreationOptions options)
    {
        if (player != Owner || cardRewards.Count == 0 ||
            !options.Flags.HasFlag(CardCreationFlags.IsCardReward) ||
            options.Flags.HasFlag(CardCreationFlags.NoCardModelModifications))
        {
            return false;
        }

        HashSet<ModelId> eligibleIds = options.GetPossibleCards(player)
            .Where(card => card.IsAllowedInRun(player.RunState) &&
                           IsRollableRarity(card.Rarity, options.RarityOdds))
            .Select(card => card.Id)
            .ToHashSet();
        if (eligibleIds.Count == 0)
        {
            return false;
        }

        HashSet<ModelId> newIds = new(eligibleIds);
        newIds.ExceptWith(cardRewards.Select(reward => reward.originalCard.Id));
        HashSet<ModelId> extraCardIds = newIds.Count > 0 ? newIds : eligibleIds;
        CardCreationOptions extraOptions = new(
            options.CardPools,
            CardCreationSource.Other,
            options.RarityOdds,
            card => extraCardIds.Contains(card.Id));
        extraOptions.WithFlags(CardCreationFlags.NoModifyHooks |
                               CardCreationFlags.NoCardPoolModifications |
                               (options.Flags & (CardCreationFlags.NoUpgrades |
                                                 CardCreationFlags.NoRarityModification)));
        if (options.RngOverride != null)
        {
            extraOptions.WithRngOverride(options.RngOverride);
        }

        CardModel? card = CardFactory.CreateForReward(player, 1, extraOptions)
            .FirstOrDefault()?.Card;
        if (card == null)
        {
            return false;
        }

        CardModel keepsakeCard = player.RunState.CloneCard(card);
        keepsakeCard.AddKeyword(JanusKeywords.Collection);
        CardCreationResult extraReward = new(card);
        extraReward.ModifyCard(keepsakeCard, this);
        cardRewards.Add(extraReward);
        return true;
    }

    private static bool IsRollableRarity(CardRarity rarity, CardRarityOddsType odds) => odds switch
    {
        CardRarityOddsType.None => false,
        CardRarityOddsType.Uniform => rarity is not (CardRarity.Basic or CardRarity.Ancient),
        _ => rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare
    };
}

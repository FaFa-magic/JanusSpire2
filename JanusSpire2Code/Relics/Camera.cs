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

        List<CardModel> possibleCards = options.GetPossibleCards(player)
            .Where(card => player.RunState.Players.Count > 1
                ? card.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly
                : card.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly)
            .ToList();
        if (possibleCards.Count == 0)
        {
            return false;
        }

        bool allowDuplicates = !possibleCards.Any(card =>
            cardRewards.All(reward => reward.originalCard.Id != card.Id));
        CardCreationOptions extraOptions = new(
            options.CardPools,
            CardCreationSource.Other,
            options.RarityOdds,
            card => (options.CardPoolFilter?.Invoke(card) ?? true) &&
                    (allowDuplicates || cardRewards.All(reward => reward.originalCard.Id != card.Id)));
        extraOptions.WithFlags(CardCreationFlags.NoModifyHooks |
                               CardCreationFlags.NoCardPoolModifications |
                               (options.Flags & CardCreationFlags.NoUpgradeRoll));
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
}

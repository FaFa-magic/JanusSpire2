using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
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

    public override bool TryModifyCardRewardOptionsLate(
        Player player,
        List<CardCreationResult> cardRewards,
        CardCreationOptions options)
    {
        if (player != Owner)
        {
            return false;
        }

        List<CardCreationResult> eligibleRewards = cardRewards
            .Where(result => !result.Card.Keywords.Contains(JanusKeywords.Collection))
            .ToList();
        if (eligibleRewards.Count == 0)
        {
            return false;
        }

        CardCreationResult? selectedReward = Owner.RunState.Rng.Niche.NextItem(eligibleRewards);
        if (selectedReward == null)
        {
            return false;
        }

        CardModel modifiedCard = Owner.RunState.CloneCard(selectedReward.Card);
        modifiedCard.AddKeyword(JanusKeywords.Collection);
        selectedReward.ModifyCard(modifiedCard, this);
        return true;
    }
}

using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class OldDiary : JanusRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Shop;
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(JanusKeywords.Record)
    ];

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (!CanAffect(card))
        {
            return Task.CompletedTask;
        }
        if (card.Owner != base.Owner)
        {
            return Task.CompletedTask;
        }
        CardCmd.ApplyKeyword(card, JanusKeywords.Record);
        return Task.CompletedTask;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (!(room is CombatRoom) || base.Owner.PlayerCombatState == null)
        {
            return Task.CompletedTask;
        }
        IEnumerable<CardModel> allCards = base.Owner.PlayerCombatState.AllCards;
        foreach (CardModel item in allCards)
        {
            if (CanAffect(item))
            {
                CardCmd.ApplyKeyword(item, JanusKeywords.Record);
            }
        }
        return Task.CompletedTask;
    }

    private static bool CanAffect(CardModel card)
    {
        if (card.Rarity == CardRarity.Basic && (card.Tags.Contains(CardTag.Strike) || card.Tags.Contains(CardTag.Defend)))
        {
            return !card.GetKeywordsWithSources(KeywordSources.Local).Contains(JanusKeywords.Record);
        }
        return false;
    }
}
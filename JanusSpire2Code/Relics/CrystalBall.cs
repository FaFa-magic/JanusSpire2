using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Runs;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class CrystalBall : JanusRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<Glam>();

    public override Task AfterObtained()
    {
        Glam glam = ModelDb.Enchantment<Glam>();
        foreach (CardModel card in PileType.Deck.GetPile(Owner).Cards
                     .Where(card => CanEnchantCommon(card, glam)))
        {
            CardCmd.Enchant<Glam>(card, 1M);
        }

        return Task.CompletedTask;
    }

    public override bool TryModifyCardRewardOptionsLate(
        Player player,
        List<CardCreationResult> cardRewards,
        CardCreationOptions options)
    {
        if (player != Owner)
        {
            return false;
        }

        EnchantValidCards(cardRewards);
        return true;
    }

    public override void ModifyMerchantCardCreationResults(
        Player player,
        List<CardCreationResult> cards)
    {
        if (player == Owner)
        {
            EnchantValidCards(cards);
        }
    }

    public override bool TryModifyCardBeingAddedToDeck(
        CardModel card,
        out CardModel? newCard)
    {
        newCard = null;
        Glam glam = ModelDb.Enchantment<Glam>();
        if (card.Owner != Owner || !CanEnchantCommon(card, glam))
        {
            return false;
        }

        newCard = EnchantCard(card);
        return true;
    }

    private void EnchantValidCards(List<CardCreationResult> options)
    {
        Glam glam = ModelDb.Enchantment<Glam>();
        foreach (CardCreationResult option in options)
        {
            CardModel card = option.Card;
            if (CanEnchantCommon(card, glam))
            {
                option.ModifyCard(EnchantCard(card), this);
            }
        }
    }

    private CardModel EnchantCard(CardModel card)
    {
        CardModel enchantedCard = Owner.RunState.CloneCard(card);
        CardCmd.Enchant<Glam>(enchantedCard, 1M);
        return enchantedCard;
    }

    private static bool CanEnchantCommon(CardModel card, Glam glam)
    {
        return card.Rarity == CardRarity.Common && glam.CanEnchant(card);
    }
}

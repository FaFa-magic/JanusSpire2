using JanusSpire2.JanusSpire2Code.Enchantment;
using JanusSpire2.JanusSpire2Code.Scripts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class MagicMirror : JanusRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(6)
    ];
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..HoverTipFactory.FromEnchantment<HallucinationEnchantment>()
    ];
    
    public override async Task AfterObtained()
    {
        CardSelectorPrefs prefs = new(
            SelectionScreenPrompt,
            0,
            DynamicVars.Cards.IntValue)
        {
            Cancelable = false,
            RequireManualConfirmation = true
        };

        foreach (CardModel item in await SelectCardsWithEnchantmentPreview(prefs))
        {
            CardCmd.Enchant<HallucinationEnchantment>(item, 1m);
            NCardEnchantVfx? nCardEnchantVfx = NCardEnchantVfx.Create(item);
            if (nCardEnchantVfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(nCardEnchantVfx);
            }
        }
    }

    private static CardTransformation CreateEnchantmentPreview(CardModel card)
    {
        CardModel preview = (CardModel)card.MutableClone();
        EnchantmentModel enchantment = ModelDb
            .Enchantment<HallucinationEnchantment>()
            .ToMutable();
        preview.EnchantInternal(enchantment, 1m);
        preview.IsEnchantmentPreview = true;
        enchantment.ModifyCard();
        return new CardTransformation(card, preview);
    }

    private Task<IEnumerable<CardModel>> SelectCardsWithEnchantmentPreview(
        CardSelectorPrefs prefs)
    {
        EnchantmentModel enchantment = ModelDb.Enchantment<HallucinationEnchantment>();
        List<CardModel> cards = Owner.Deck.Cards
            .Where(card => card.Type != CardType.Quest &&
                           card.IsTransformable &&
                           enchantment.CanEnchant(card))
            .ToList();
        return DeckComparisonSelectCmd.Select(Owner, cards, CreateEnchantmentPreview, prefs);
    }
}

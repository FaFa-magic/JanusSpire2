using JanusSpire2.JanusSpire2Code.Enchantment;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

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

    private async Task<IEnumerable<CardModel>> SelectCardsWithEnchantmentPreview(
        CardSelectorPrefs prefs)
    {
        EnchantmentModel enchantment = ModelDb.Enchantment<HallucinationEnchantment>();
        List<CardModel> cards = Owner.Deck.Cards
            .Where(card => card.Type != CardType.Quest &&
                           card.IsTransformable &&
                           enchantment.CanEnchant(card))
            .ToList();
        if (Owner.Creature.IsDead || cards.Count == 0)
        {
            return [];
        }

        if (!prefs.RequireManualConfirmation && cards.Count <= prefs.MinSelect)
        {
            return cards;
        }

        if (CardSelectCmd.Selector is not null)
        {
            return await CardSelectCmd.Selector.GetSelectedCards(
                cards,
                prefs.MinSelect,
                prefs.MaxSelect);
        }

        uint choiceId = RunManager.Instance.PlayerChoiceSynchronizer.ReserveChoiceId(Owner);
        bool selectsLocally = LocalContext.IsMe(Owner) &&
                              RunManager.Instance.NetService.Type != NetGameType.Replay;
        if (!selectsLocally)
        {
            return (await RunManager.Instance.PlayerChoiceSynchronizer
                .WaitForRemoteChoice(Owner, choiceId)).AsDeckCards();
        }

        IEnumerable<CardModel> selectedCards;
        if (CardSelectCmd.LocalSelector is not null)
        {
            selectedCards = await CardSelectCmd.LocalSelector.GetSelectedCards(
                cards,
                prefs.MinSelect,
                prefs.MaxSelect);
        }
        else
        {
            NDeckTransformSelectScreen screen = NDeckTransformSelectScreen.ShowScreen(
                cards,
                CreateEnchantmentPreview,
                prefs);
            selectedCards = await screen.CardsSelected();
        }

        List<CardModel> result = selectedCards.ToList();
        RunManager.Instance.PlayerChoiceSynchronizer.SyncLocalChoice(
            Owner,
            choiceId,
            PlayerChoiceResult.FromMutableDeckCards(result));
        return result;
    }
}

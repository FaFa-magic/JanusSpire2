using JanusSpire2.JanusSpire2Code.Cards.Basic;
using JanusSpire2.JanusSpire2Code.Cards.Token;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class SmokeFormationTactics() : JanusCardModel(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ModCardVars.Int("UpgradeTimes", 1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.PlayerCombatState is null) return;

        List<CardModel> smokeCards = new();

        var strikeCards = Owner.PlayerCombatState.AllPiles
            .SelectMany(p => p.Cards)
            .Where(c => c is Strike)
            .ToList();

        foreach (var original in strikeCards)
        {
            CardModel newCard = await CreateSmokeCardFromOriginal<SmokeStrike>(original);
            await CardCmd.Transform(original, newCard);

            smokeCards.Add(newCard);
        }

        var defendCards = Owner.PlayerCombatState.AllPiles
            .SelectMany(p => p.Cards)
            .Where(c => c is Defend)
            .ToList();

        foreach (var original in defendCards)
        {
            CardModel newCard = await CreateSmokeCardFromOriginal<SmokeDefend>(original);
            await CardCmd.Transform(original, newCard);

            smokeCards.Add(newCard);
        }

        int times = DynamicVars["UpgradeTimes"].IntValue;

        foreach (var card in smokeCards)
        {
            for (int i = 0; i < times; i++)
            {
                CardCmd.Upgrade(card);
            }
        }
    }
    
    private Task<CardModel> CreateSmokeCardFromOriginal<TNewCard>(CardModel original) where TNewCard : CardModel, new()
    {
        CardModel newCard = CombatState!.CreateCard<TNewCard>(Owner);

        if (original.IsUpgraded && newCard.IsUpgradable)
        {
            for(int i = 0; i < original.CurrentUpgradeLevel; i++) { CardCmd.Upgrade(newCard); }
        }

        if (original.Enchantment != null)
        {
            EnchantmentModel enchantmentModel = (EnchantmentModel)original.Enchantment.MutableClone();

            if (enchantmentModel.CanEnchant(newCard))
            {
                CardCmd.Enchant(enchantmentModel, newCard, enchantmentModel.Amount);
            }
        }

        return Task.FromResult(newCard);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["UpgradeTimes"].UpgradeValueBy(1m);
    }    
}
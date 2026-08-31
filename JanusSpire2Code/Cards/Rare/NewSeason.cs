using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class NewSeason() : JanusCardModel(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    private const string SwiftAmountKey = "SwiftAmount";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Transcribe];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(SwiftAmountKey, 1M)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(JanusKeywords.Sticker),
        ..HoverTipFactory.FromEnchantment<Swift>(DynamicVars[SwiftAmountKey].IntValue)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel copy = CreateClone();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(copy, MainFile.Diary, Owner),
            0.2F);
    }

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (Pile?.Type != MainFile.Diary ||
            creator != Owner ||
            Owner.Creature.IsDead ||
            card.CombatState == null ||
            !card.Keywords.Contains(JanusKeywords.Sticker) ||
            card.Enchantment is not null and not Swift)
        {
            return Task.CompletedTask;
        }

        Swift swift = ModelDb.Enchantment<Swift>();
        if (swift.CanEnchant(card))
        {
            CardCmd.Enchant<Swift>(card, DynamicVars[SwiftAmountKey].BaseValue);
        }

        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

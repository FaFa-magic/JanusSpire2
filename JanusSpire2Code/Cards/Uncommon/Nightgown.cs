using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class Nightgown() : JanusCardModel(2, CardType.Power, CardRarity.Uncommon, TargetType.Self), ICardDescriptionContributor
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Transcribe];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(8),
        ModCardVars.Int("Nightgown", 1),
        new CalculationBaseVar(0M),
        new CalculationExtraVar(1M),
        new CalculatedBlockVar(ValueProp.Unpowered | ValueProp.Move)
            .WithMultiplier(CalculateCurrentBlock)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Block)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel copy = CreateClone();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(copy, MainFile.Diary, Owner),
            0.2F);
    }

    public override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        if (Pile?.Type == MainFile.Diary && card.Owner == Owner)
        {
            this.RequestVisualReload();
        }

        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner.Creature) ||
            Owner.Creature.IsDead ||
            Pile?.Type != MainFile.Diary)
        {
            return;
        }

        decimal block = DynamicVars.CalculatedBlock.Calculate(null);
        if (block <= 0M)
        {
            return;
        }

        await CreatureCmd.TriggerAnim(Owner.Creature, "BlockStart", 0.3F);
        await CreatureCmd.GainBlock(
            Owner.Creature,
            block,
            ValueProp.Unpowered | ValueProp.Move,
            null);
    }

    public IEnumerable<CardDescriptionFragment> GetDescriptionFragments(CardDescriptionContext context)
    {
        bool isInDiary = Pile?.Type == MainFile.Diary ||
                         (Pile == null && context.PileType == MainFile.Diary);
        if (!isInDiary)
        {
            return [];
        }

        return [
            new CardDescriptionFragment(
                new LocString("cards", $"{Id.Entry}.currentBlockDescription"),
                CardDescriptionFragmentPlacement.AfterBase)
        ];
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    private static decimal CalculateCurrentBlock(CardModel card, Creature? target)
    {
        int cardsPerBlock = card.DynamicVars.Cards.IntValue;
        if (cardsPerBlock <= 0 || card.Owner.PlayerCombatState == null)
        {
            return 0M;
        }

        int realCardCount = card.Owner.PlayerCombatState.AllCards
            .Count(otherCard => otherCard is not JanusRecordMappingCard);
        int cardGroups = realCardCount / cardsPerBlock;
        return cardGroups * card.DynamicVars["Nightgown"].BaseValue;
    }
}

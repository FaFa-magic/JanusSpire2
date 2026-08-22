using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class Nightgown() : JanusCardModel(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Transcribe];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(8),
        ModCardVars.Int("Nightgown", 1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Block)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel copy = CreateClone();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(copy, MainFile.Diary, Owner),
            0.2f);
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

        int cardsPerBlock = DynamicVars.Cards.IntValue;
        if (cardsPerBlock <= 0)
        {
            return;
        }

        int totalCards = Owner.PlayerCombatState?.AllCards.Count() ?? 0;
        decimal block = totalCards / cardsPerBlock * DynamicVars["Nightgown"].BaseValue;
        if (block <= 0M)
        {
            return;
        }

        await CreatureCmd.TriggerAnim(Owner.Creature, "BlockStart", 0.3f);
        await CreatureCmd.GainBlock(
            Owner.Creature,
            block,
            ValueProp.Unpowered | ValueProp.Move,
            null);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
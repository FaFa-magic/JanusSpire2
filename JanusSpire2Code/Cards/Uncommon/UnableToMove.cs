using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class UnableToMove() : JanusCardModel(-1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    public override bool GainsBlock => true;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Ethereal];
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6M, ValueProp.Move | ValueProp.Unpowered)];
    
    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (card != this || Owner.Creature.IsDead)
        {
            return;
        }

        await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move | ValueProp.Unpowered, null);
    }

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal)
    {
        if (card != this || Owner.Creature.IsDead)
        {
            return;
        }

        CardModel copy = CreateClone();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Discard, Owner),
            0.2f);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3M);
}

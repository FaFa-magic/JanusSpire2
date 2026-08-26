using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class ChangingTime() : JanusCardModel(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2),
        new EnergyVar(2),
        new BlockVar(8M, ValueProp.Move | ValueProp.Unpowered)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Record];
    
    public override async Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        if (card != this || Owner.Creature.IsDead || card.Pile == null)
        {
            return;
        }

        if (card.Pile.Type == PileType.Hand)
        {
            await DrawCardsWithHookContext();
        }
        else if (card.Pile.Type == MainFile.Diary)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        }
        else if (card.Pile.Type == PileType.Exhaust)
        {
            await CreatureCmd.TriggerAnim(Owner.Creature, "BlockStart", 0.3f);
            await CreatureCmd.GainBlock(
                Owner.Creature,
                DynamicVars.Block.BaseValue,
                ValueProp.Move | ValueProp.Unpowered,
                null);
        }
    }

    private async Task DrawCardsWithHookContext()
    {
        if (!LocalContext.NetId.HasValue)
        {
            await CardPileCmd.Draw(
                new ThrowingPlayerChoiceContext(),
                DynamicVars.Cards.IntValue,
                Owner);
            return;
        }

        var choiceContext = new HookPlayerChoiceContext(
            Owner,
            LocalContext.NetId.Value,
            GameActionType.Combat);
        Task drawTask = CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        await choiceContext.AssignTaskAndWaitForPauseOrCompletion(drawTask);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1M);
    }
}

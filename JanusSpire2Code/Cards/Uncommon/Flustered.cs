using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class Flustered() : JanusCardModel(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;
    
    public override bool GainsBlock => true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7M, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<GoodTimesPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);

        int amount = ResolveEnergyXValue();
        await PowerCmd.Apply<GoodTimesPower>(
            choiceContext,
            Owner.Creature,
            amount,
            Owner.Creature,
            this);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        
        List<CardModel> generatedCards = new(amount);
        for (int i = 0; i < amount; i++)
        {
            CardModel generatedCard = CombatState.CreateCard<Flustered>(Owner);
            if (IsUpgraded)
            {
                CardCmd.Upgrade(generatedCard);
            }
            generatedCard.AddKeyword(JanusKeywords.Record);
            generatedCards.Add(generatedCard);
        }

        await CardPileCmd.AddGeneratedCardsToCombat(generatedCards, PileType.Hand, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2M);
}

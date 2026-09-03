using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class Flustered() : JanusCardModel(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;
    
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Record];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ModCardVars.Int("Flustered", 3),
        new BlockVar(7M, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<GoodTimesPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);

        await PowerCmd.Apply<GoodTimesPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Flustered"].BaseValue,
            Owner.Creature,
            this);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        int amount = ResolveEnergyXValue();
        List<CardModel> generatedCards = new(amount);
        for (int i = 0; i < amount; i++)
        {
            CardModel generatedCard = CombatState.CreateCard<Flustered>(Owner);
            if (IsUpgraded)
            {
                CardCmd.Upgrade(generatedCard);
            }

            generatedCards.Add(generatedCard);
        }

        await CardPileCmd.AddGeneratedCardsToCombat(generatedCards, PileType.Hand, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2M);
}

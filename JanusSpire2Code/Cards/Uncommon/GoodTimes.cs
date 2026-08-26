using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Combat.HandSize;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class GoodTimes() : JanusCardModel(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Transcribe];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<GoodTimesPower>()];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ModCardVars.Int("GoodTimes", 1),
        new EnergyVar(1),
        new CardsVar(0)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel card = CreateClone();
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, MainFile.Diary, base.Owner), 0.2f);

        if (DynamicVars.Cards.IntValue > 0)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        }
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (Pile?.Type != MainFile.Diary ||
            card.Owner != Owner ||
            Owner.Creature.IsDead ||
            CombatState == null)
        {
            return;
        }

        List<GoodTimes> activeCopies = MainFile.Diary.GetPile(Owner).Cards
            .OfType<GoodTimes>()
            .ToList();
        if (activeCopies.Count == 0 || activeCopies[0] != this)
        {
            return;
        }

        int handSize = PileType.Hand.GetPile(Owner).Cards.Count;
        int maxHandSize = MaxHandSizeCalculator.Calculate(Owner);
        if (handSize != maxHandSize)
        {
            return;
        }

        decimal maxHandSizeIncrease = activeCopies.Sum(copy => copy.DynamicVars["GoodTimes"].BaseValue);
        decimal energyGain = activeCopies.Sum(copy => copy.DynamicVars.Energy.BaseValue);

        await PowerCmd.Apply<GoodTimesPower>(
            choiceContext,
            Owner.Creature,
            maxHandSizeIncrease,
            Owner.Creature,
            this);
        await PlayerCmd.GainEnergy(energyGain, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(3M);
}

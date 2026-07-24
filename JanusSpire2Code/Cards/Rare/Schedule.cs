using JanusSpire2.JanusSpire2Code.Cards.Token;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class Schedule() : JanusCardModel(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ModCardVars.Int("BattlePlan", 3),
        new EnergyVar(3),
        new CardsVar(5)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<StickyNotes>(false)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BattlePlanPower>(choiceContext, base.Owner.Creature, DynamicVars["BattlePlan"].BaseValue, base.Owner.Creature, this);
        await PowerCmd.Apply<EnergyDebuffPower>(choiceContext, base.Owner.Creature, DynamicVars.Energy.BaseValue, base.Owner.Creature, this);
        await PowerCmd.Apply<CardDebuffPower>(choiceContext, base.Owner.Creature, DynamicVars.Cards.BaseValue, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
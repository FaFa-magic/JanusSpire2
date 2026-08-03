using JanusSpire2.JanusSpire2Code.Orbs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class Courage() : JanusCardModel(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("Orbs", 1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<CourageOrb>()
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await OrbCmd.Channel<CourageOrb>(choiceContext, cardPlay.Card.Owner);
        if (base.IsUpgraded)
        {
            await OrbCmd.Channel<CourageOrb>(choiceContext, cardPlay.Card.Owner);
        }
    }
    
    protected override void OnUpgrade()
    {
        base.DynamicVars["Orbs"].UpgradeValueBy(1M);
    }
}
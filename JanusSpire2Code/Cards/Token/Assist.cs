using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace JanusSpire2.JanusSpire2Code.Cards.Token;

[RegisterCard(typeof(TokenCardPool))]
public sealed class Assist() : JanusRecordCardModel(0, CardType.Skill, CardRarity.Token, TargetType.AnyAlly)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1),
        new CardsVar(1)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        ArgumentNullException.ThrowIfNull(cardPlay.Target.Player, "cardPlay.Target.Player");
        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, cardPlay.Target.Player);
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.IntValue, cardPlay.Target.Player);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Energy.UpgradeValueBy(1M);
        base.DynamicVars.Cards.UpgradeValueBy(1M);
    }
}
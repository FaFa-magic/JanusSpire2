using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class Courage() : JanusCardModel(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [ModCardVars.Int("BlackCatSeal", 3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {

    }

    protected override void OnUpgrade() => DynamicVars["BlackCatSeal"].UpgradeValueBy(1M);
}
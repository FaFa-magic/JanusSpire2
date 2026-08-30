using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Token;

public sealed class SmokeEmitter() : JanusTokenCardModel(0, CardType.Skill, CardRarity.Token, TargetType.AnyPlayer)
{
    public override int MaxUpgradeLevel => 999;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust, JanusKeywords.Sticker];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ModCardVars.Int("Smoke", 1)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Creature target = cardPlay.Target ?? Owner.Creature;
        await PowerCmd.Apply<SmokePower>(
            choiceContext,
            target,
            DynamicVars["Smoke"].BaseValue,
            Owner.Creature,
            this);
    }
    
    protected override void OnUpgrade() => DynamicVars["Smoke"].UpgradeValueBy(1M);
}

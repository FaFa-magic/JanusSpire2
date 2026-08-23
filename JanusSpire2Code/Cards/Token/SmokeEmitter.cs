using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Combat.CardTargeting;

namespace JanusSpire2.JanusSpire2Code.Cards.Token;

public sealed class SmokeEmitter() : JanusTokenCardModel(0, CardType.Skill, CardRarity.Token, CustomTargetType.Anyone)
{
    public override int MaxUpgradeLevel => 999;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust, JanusKeywords.Sticker];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ModCardVars.Int("Smoke", 1)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        await PowerCmd.Apply<SmokePower>(choiceContext, cardPlay.Target, DynamicVars["Smoke"].BaseValue, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade() => DynamicVars["Smoke"].UpgradeValueBy(1M);
}

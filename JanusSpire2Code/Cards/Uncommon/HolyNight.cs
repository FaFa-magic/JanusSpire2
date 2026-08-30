using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class HolyNight() : JanusCardModel(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Inspiration];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ModCardVars.Int("StrengthLoss", 4),
        ModCardVars.Int(InspirationKeyword.AmountVar, 3),
        new DamageVar(4M, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await PowerCmd.Apply<HolyNightPower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["StrengthLoss"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2M);
        DynamicVars["StrengthLoss"].UpgradeValueBy(2M);
    }
}

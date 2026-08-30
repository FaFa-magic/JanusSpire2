using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Common;

public sealed class Frolic() : JanusCardModel(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private const string IncreaseVar = "Increase";
    private decimal _extraDamageFromPlays;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Inspiration];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8M, ValueProp.Move),
        ModCardVars.Int(IncreaseVar, 6),
        ModCardVars.Int(InspirationKeyword.AmountVar, 2)
    ];

    private decimal ExtraDamageFromPlays
    {
        get => _extraDamageFromPlays;
        set
        {
            AssertMutable();
            _extraDamageFromPlays = value;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        decimal increase = DynamicVars[IncreaseVar].BaseValue;
        DynamicVars.Damage.BaseValue += increase;
        ExtraDamageFromPlays += increase;
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue += ExtraDamageFromPlays;
    }

    protected override void OnUpgrade() => DynamicVars[IncreaseVar].UpgradeValueBy(2M);
}

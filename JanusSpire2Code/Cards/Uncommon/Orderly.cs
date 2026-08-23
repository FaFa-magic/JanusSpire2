using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class Orderly() : JanusCardModel(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private const int BaseHitCount = 2;
    private const string CalculatedHitsKey = "CalculatedHits";

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(3M, ValueProp.Move),
        new CalculationBaseVar(BaseHitCount),
        new CalculationExtraVar(1M),
        new CalculatedVar(CalculatedHitsKey).WithMultiplier(CountGeneratedCards)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount((int)((CalculatedVar)DynamicVars[CalculatedHitsKey]).Calculate(cardPlay.Target))
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator == Owner && CombatState != null)
        {
            this.RequestVisualReload();
        }

        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1M);

    private static decimal CountGeneratedCards(CardModel card, Creature? target)
    {
        return CombatManager.Instance.History.Entries
            .OfType<CardGeneratedEntry>()
            .Count(entry => entry.Creator == card.Owner);
    }
}

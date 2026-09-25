using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards.Common;

public sealed class EightBall() : JanusCardModel(8, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    private const int AttackThreshold = 8;
    private const string AttacksPlayedKey = "AttacksPlayed";

    private int _attacksPlayed;

    [SavedProperty]
    public int AttacksPlayed
    {
        get => _attacksPlayed;
        private set
        {
            AssertMutable();
            _attacksPlayed = Math.Clamp(value, 0, AttackThreshold - 1);
            DynamicVars[AttacksPlayedKey].BaseValue = _attacksPlayed;
            this.RequestVisualReload();
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Record];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(10M, ValueProp.Move),
        new CardsVar(AttackThreshold),
        new DynamicVar(AttacksPlayedKey, AttacksPlayed)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player != Owner ||
            cardPlay.Card.Type != CardType.Attack ||
            cardPlay.Card is EightBall)
        {
            return;
        }

        int attackThreshold = Math.Max(1, DynamicVars.Cards.IntValue);
        int nextCount = AttacksPlayed + 1;
        bool shouldAutoPlay = nextCount >= attackThreshold;
        AttacksPlayed = shouldAutoPlay ? 0 : nextCount;
        if (!shouldAutoPlay)
        {
            return;
        }

        if (CombatManager.Instance.IsOverOrEnding || Owner.Creature.IsDead)
        {
            return;
        }

        CardModel cardToPlay = Pile?.Type == PileType.Play
            ? CreateDupe(Owner)
            : this;
        await CardCmd.AutoPlay(choiceContext, cardToPlay, null);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4M);
}

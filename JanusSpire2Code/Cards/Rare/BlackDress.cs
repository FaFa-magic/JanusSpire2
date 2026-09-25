using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Combat;
using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class BlackDress() : JanusCardModel(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Record];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9M, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [.. HoverTipFactory.FromRelic<RelicFragment>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        bool isInDiary = Pile?.Type == MainFile.Diary;
        bool isFinishingPlayIntoDiary = Pile?.Type == PileType.Play &&
            CombatManager.Instance.History.CardPlaysStarted
                .LastOrDefault(entry => ReferenceEquals(entry.CardPlay.Card, this))?
                .CardPlay.ResultPile == MainFile.Diary;
        if (isInDiary || isFinishingPlayIntoDiary)
        {
            RelicModel fragment = ModelDb.Relic<RelicFragment>().ToMutable();
            room.AddExtraReward(Owner, new RelicReward(fragment, Owner));
        }

        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => base.EnergyCost.UpgradeBy(-1);
}

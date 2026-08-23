using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class Involuntarily() : JanusCardModel(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    private Creature? _pendingTarget;
    private decimal _pendingSealAmount;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Transcribe];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ModCardVars.Int("Involuntarily", 30)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<BlackCatSealPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel copy = CreateClone();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(copy, MainFile.Diary, Owner),
            0.2f);
    }

    public override decimal ModifyHpLostAfterOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Pile?.Type != MainFile.Diary ||
            cardSource?.Owner != Owner ||
            cardSource.Pile?.Type != PileType.Play ||
            target.Side != CombatSide.Enemy ||
            amount <= 0M)
        {
            return amount;
        }

        List<Involuntarily> activeCopies = MainFile.Diary.GetPile(Owner).Cards
            .OfType<Involuntarily>()
            .ToList();
        if (activeCopies.Count == 0 || !ReferenceEquals(activeCopies[0], this))
        {
            return amount;
        }

        decimal totalPercentage = activeCopies.Sum(
            card => card.DynamicVars["Involuntarily"].BaseValue);
        _pendingTarget = target;
        _pendingSealAmount = Math.Floor(amount * totalPercentage / 100M);
        return 0M;
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        Creature? target = _pendingTarget;
        decimal sealAmount = _pendingSealAmount;
        _pendingTarget = null;
        _pendingSealAmount = 0M;

        if (target == null ||
            sealAmount <= 0M ||
            Owner.Creature.IsDead ||
            target.IsDead ||
            target.CombatState == null)
        {
            return;
        }

        await PowerCmd.Apply<BlackCatSealPower>(
            new ThrowingPlayerChoiceContext(),
            target,
            sealAmount,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() =>
        DynamicVars["Involuntarily"].UpgradeValueBy(10M);
}

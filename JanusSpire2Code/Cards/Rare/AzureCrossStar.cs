using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class AzureCrossStar() : JanusCardModel(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private bool _copiedThisTurn;

    [SavedProperty]
    public bool CopiedThisTurn
    {
        get => _copiedThisTurn;
        private set
        {
            AssertMutable();
            if (_copiedThisTurn == value)
            {
                return;
            }

            _copiedThisTurn = value;
            this.RequestVisualReload();
        }
    }
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(4M, ValueProp.Move)
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
        if (CopiedThisTurn
            || Pile?.Type != PileType.Hand
            || cardPlay.Player != Owner
            || cardPlay.Card is AzureCrossStar)
        {
            return;
        }

        CopiedThisTurn = true;
        CardModel copy = CreateClone();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner),
            0.2F);
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _copiedThisTurn = false;
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add(nameof(CopiedThisTurn), CopiedThisTurn);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1M);
}

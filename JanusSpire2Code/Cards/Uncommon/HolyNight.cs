using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interactions.RightClick;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class HolyNight() : JanusCardModel(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy), IModRightClickableCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ModCardVars.Int("StrengthLoss", 4),
        new CardsVar(3),
        new DamageVar(4m, ValueProp.Move)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await PowerCmd.Apply<HolyNightPower>(choiceContext, cardPlay.Target, base.DynamicVars["StrengthLoss"].BaseValue, base.Owner.Creature, this);
    }

    public bool CanHandleRightClickLocal(ModRightClickContext context)
    {
        return context.Trigger.Source == ModRightClickSource.CombatPileCard &&
               context.Trigger.ExpectedCardPile is { } expectedPile &&
               IsSupportedRightClickPile(expectedPile);
    }

    public bool CanExecuteRightClick(ModRightClickExecutionContext context)
    {
        if (context.Model != this ||
            context.Player != Owner ||
            context.Trigger.Source != ModRightClickSource.CombatPileCard)
        {
            return false;
        }

        if (Pile?.Type != MainFile.Diary &&
            (context.Trigger.ExpectedCardPile is not { } expectedPile ||
             !IsSupportedRightClickPile(expectedPile) ||
             Pile?.Type != expectedPile))
        {
            return false;
        }

        CardPile hand = PileType.Hand.GetPile(Owner);
        CardPile? diaryPile = Owner.PlayerCombatState?.AllPiles.FirstOrDefault(p => p.Type == MainFile.Diary);

        return context.PlayerChoiceContext != null &&
               hand.Cards.Count < CardPile.MaxCardsInHand &&
               diaryPile != null &&
               diaryPile.Cards.Count >= DynamicVars.Cards.IntValue;
    }

    private static bool IsSupportedRightClickPile(PileType pileType)
    {
        return pileType is PileType.Draw or PileType.Discard or PileType.Exhaust || pileType == MainFile.Diary;
    }

    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        if (Pile?.Type == PileType.Hand)
        {
            return;
        }

        CardPile hand = PileType.Hand.GetPile(base.Owner);
        CardPile? diaryPile = base.Owner.PlayerCombatState?.AllPiles.FirstOrDefault(p => p.Type == MainFile.Diary);

        if (hand.Cards.Count >= CardPile.MaxCardsInHand ||
            diaryPile == null ||
            diaryPile.Cards.Count < DynamicVars.Cards.IntValue)
        {
            return;
        }

        PlayerChoiceContext? playerChoiceContext = context.PlayerChoiceContext;
        if (playerChoiceContext == null)
        {
            return;
        }

        if (NCapstoneContainer.Instance != null && NCapstoneContainer.Instance.InUse)
        {
            NCapstoneContainer.Instance.Close();
        }

        var selected = (await CardSelectCmd.FromCombatPile(
            playerChoiceContext,
            diaryPile,
            base.Owner,
            new CardSelectorPrefs(
                CardSelectorPrefs.ExhaustSelectionPrompt,
                DynamicVars.Cards.IntValue
            ))).ToList();

        foreach (var item in selected)
        {
            await CardCmd.Exhaust(playerChoiceContext, item);
        }

        await CardPileCmd.Add(this, PileType.Hand);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2M);
        DynamicVars["StrengthLoss"].UpgradeValueBy(2M);
    }
}

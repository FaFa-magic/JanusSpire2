using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class PilotLamp() : JanusCardModel(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(16M, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        var combatState = CombatState;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (CombatManager.Instance.IsOverOrEnding || Owner.Creature.IsDead ||
            !ReferenceEquals(CombatState, combatState))
            return;

        CardPile diary = MainFile.Diary.GetPile(Owner);
        if (diary.Cards.Count == 0)
        {
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            diary.Cards.ToList(),
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        if (selected == null || CombatManager.Instance.IsOverOrEnding || Owner.Creature.IsDead ||
            !ReferenceEquals(CombatState, combatState) || !ReferenceEquals(selected.Pile, diary))
        {
            return;
        }

        CardModel copy = selected.CreateClone();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner),
            0.2f);
    }
    
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6M);
}

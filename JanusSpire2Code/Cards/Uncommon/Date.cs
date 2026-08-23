using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class Date() : JanusCardModel(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private const int GeneratedCardsThreshold = 3;
    private const string GeneratedCardsKey = "GeneratedCards";

    private int _generatedCards;

    [SavedProperty]
    public int GeneratedCards
    {
        get => _generatedCards;
        private set
        {
            AssertMutable();
            _generatedCards = Math.Clamp(value, 0, GeneratedCardsThreshold - 1);
            DynamicVars[GeneratedCardsKey].BaseValue = _generatedCards;
            this.RequestVisualReload();
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Perk];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10M, ValueProp.Move),
        new CardsVar(2),
        ModCardVars.Int("Date", GeneratedCardsThreshold),
        new DynamicVar(GeneratedCardsKey, 0)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (CombatManager.Instance.IsOverOrEnding || Owner.Creature.IsDead)
        {
            return;
        }

        int selectionCount = Math.Min(
            DynamicVars.Cards.IntValue,
            PileType.Hand.GetPile(Owner).Cards.Count);
        if (selectionCount <= 0)
        {
            return;
        }

        IEnumerable<CardModel> selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, selectionCount),
            null,
            this);
        await CardPileCmd.Add(selectedCards, MainFile.Diary);
    }

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator != Owner || Owner.Creature.IsDead || CombatState == null)
        {
            return;
        }

        int nextProgress = GeneratedCards + 1;
        if (nextProgress < GeneratedCardsThreshold)
        {
            GeneratedCards = nextProgress;
            return;
        }

        GeneratedCards = 0;
        if (Pile?.IsCombatPile == true && Pile.Type != PileType.Hand)
        {
            await CardPileCmd.Add(this, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4M);
}

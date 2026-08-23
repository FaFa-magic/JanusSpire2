using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class CatInBox : JanusRelicModel
{
    private const int TriggerCardCount = 3;

    private int _cardsPlayedThisTurn;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;

    public override int DisplayAmount => _cardsPlayedThisTurn;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.Transform)
    ];

    private int CardsPlayedThisTurn
    {
        get => _cardsPlayedThisTurn;
        set
        {
            AssertMutable();
            _cardsPlayedThisTurn = value;
            Status = value == TriggerCardCount - 1
                ? RelicStatus.Active
                : RelicStatus.Normal;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature))
        {
            CardsPlayedThisTurn = 0;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!CombatManager.Instance.IsInProgress || cardPlay.Card.Owner != Owner)
        {
            return;
        }

        CardsPlayedThisTurn++;
        if (CardsPlayedThisTurn != TriggerCardCount)
        {
            return;
        }

        List<CardTransformation> transformations = PileType.Hand
            .GetPile(Owner)
            .Cards
            .Where(card => card.IsTransformable)
            .Select(card => new CardTransformation(card))
            .ToList();
        if (transformations.Count == 0)
        {
            return;
        }

        Flash();
        await CardCmd.Transform(
            transformations,
            Owner.RunState.Rng.CombatCardSelection,
            CardPreviewStyle.None);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        CardsPlayedThisTurn = 0;
        return Task.CompletedTask;
    }
}

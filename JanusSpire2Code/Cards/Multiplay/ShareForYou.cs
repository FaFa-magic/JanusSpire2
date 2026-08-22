using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Multiplay;

public sealed class ShareForYou() : JanusCardModel(1, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);

        List<CardModel> diaryCards = MainFile.Diary.GetPile(Owner).Cards.ToList();
        if (diaryCards.Count == 0)
        {
            return;
        }

        ulong localPlayerId = LocalContext.NetId ?? Owner.NetId;
        foreach (Player ally in CombatState.Players.Where(player =>
                     player != Owner && player.Creature.IsAlive))
        {
            BranchingPlayerChoiceContext allyChoiceContext = new(
                this,
                localPlayerId,
                GameActionType.Combat,
                choiceContext);
            Task shareTask = ShareWithAlly(allyChoiceContext, ally, diaryCards);
            await allyChoiceContext.AssignTaskAndWaitForPauseOrCompletion(shareTask);
        }
    }

    private async Task ShareWithAlly(
        PlayerChoiceContext choiceContext,
        Player ally,
        IReadOnlyList<CardModel> diaryCards)
    {
        CardModel? selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            diaryCards,
            ally,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        if (selected == null || CombatManager.Instance.IsOverOrEnding || ally.Creature.IsDead)
        {
            return;
        }

        CardModel copy = selected.CreateCloneForPlayer(ally);
        await CardPileCmd.AddGeneratedCardsToCombat([copy], PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Runs;

namespace JanusSpire2.JanusSpire2Code.Scripts;

internal static class DeckComparisonSelectCmd
{
    // The vanilla transformation command cannot filter eligible cards beyond IsTransformable.
    internal static async Task<IEnumerable<CardModel>> Select(
        Player player,
        IReadOnlyList<CardModel> cards,
        Func<CardModel, CardTransformation> createPreview,
        CardSelectorPrefs prefs)
    {
        if (player.Creature.IsDead || cards.Count == 0)
        {
            return [];
        }

        if (!prefs.RequireManualConfirmation && cards.Count <= prefs.MinSelect)
        {
            return cards;
        }

        RunManager run = RunManager.Instance;
        if (run.CardSelectorStack.GetSelector(player) is { } selector)
        {
            return await selector.GetSelectedCards(cards, prefs.MinSelect, prefs.MaxSelect);
        }

        var synchronizer = run.PlayerChoiceSynchronizer;
        uint choiceId = synchronizer.ReserveChoiceId(player);
        if (!LocalContext.IsMe(player) || run.NetService.Type == NetGameType.Replay)
        {
            return (await synchronizer.WaitForRemoteChoice(player, choiceId)).AsDeckCards();
        }

        IEnumerable<CardModel> selected = run.CardSelectorStack.LocalSelector is { } localSelector
            ? await localSelector.GetSelectedCards(cards, prefs.MinSelect, prefs.MaxSelect)
            : await NDeckTransformSelectScreen.ShowScreen(cards, createPreview, prefs).CardsSelected();
        List<CardModel> result = selected.ToList();
        synchronizer.SyncLocalChoice(player, choiceId, PlayerChoiceResult.FromMutableDeckCards(result));
        return result;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class FlowersInTheMirrorPower : JanusPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile =>
        ContentAssetProfiles.Power("CreativeAiPower");

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player != Owner.Player)
        {
            return;
        }

        List<CardModel> generatedCards = GetCardsGeneratedDirectlyBy(cardPlay);
        if (generatedCards.Count == 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Decrement(this);

        if (Owner.IsDead || !CombatManager.Instance.IsInProgress)
        {
            return;
        }

        List<CardModel> copies = generatedCards
            .Select(card => card.CreateClone())
            .ToList();

        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardsToCombat(copies, MainFile.Diary, Owner.Player));
    }

    private static List<CardModel> GetCardsGeneratedDirectlyBy(CardPlay cardPlay)
    {
        List<CombatHistoryEntry> entries = CombatManager.Instance.History.Entries.ToList();
        int finishIndex = entries.FindLastIndex(entry =>
            entry is CardPlayFinishedEntry finished && ReferenceEquals(finished.CardPlay, cardPlay));
        if (finishIndex < 0)
        {
            return [];
        }

        int startIndex = entries.FindLastIndex(
            finishIndex - 1,
            entry => entry is CardPlayStartedEntry started && ReferenceEquals(started.CardPlay, cardPlay));
        if (startIndex < 0)
        {
            return [];
        }

        List<CardModel> generatedCards = [];
        int nestedPlayDepth = 0;
        for (int i = startIndex + 1; i < finishIndex; i++)
        {
            switch (entries[i])
            {
                case CardPlayStartedEntry:
                    nestedPlayDepth++;
                    break;
                case CardPlayFinishedEntry:
                    nestedPlayDepth--;
                    break;
                case CardGeneratedEntry generated
                    when nestedPlayDepth == 0 && generated.Creator == cardPlay.Player:
                    generatedCards.Add(generated.Card);
                    break;
            }
        }

        return generatedCards;
    }
}

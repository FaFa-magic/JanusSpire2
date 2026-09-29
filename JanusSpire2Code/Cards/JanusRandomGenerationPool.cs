using JanusSpire2.JanusSpire2Code.Characters;
using JanusSpire2.JanusSpire2Code.Configs;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Runs;

namespace JanusSpire2.JanusSpire2Code.Cards;

internal static class JanusRandomGenerationPool
{
    internal static IEnumerable<CardModel> Filter(IEnumerable<CardModel> candidates, IRunState runState) =>
        JanusConfigPage.RestrictRandomGeneration(runState)
            ? candidates.Where(IsBaseCharacterOrJanusCard)
            : candidates;

    internal static bool IsBaseCharacterOrJanusCard(CardModel card)
    {
        if (card.Pool is JanusCardPool)
        {
            return card.GetType().Assembly == typeof(JanusCardModel).Assembly;
        }

        return card.GetType().Assembly == typeof(CardModel).Assembly &&
               card.Pool is IroncladCardPool or SilentCardPool or DefectCardPool or
                   NecrobinderCardPool or RegentCardPool;
    }
}

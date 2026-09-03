using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class LittleDevil() : JanusRecordCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, JanusKeywords.Record];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0M),
        new CalculationExtraVar(1M),
        new CalculatedVar("Damage").WithMultiplier(CountDiaryCards)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<BlackCatSealPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        decimal amount = ((CalculatedVar)DynamicVars["Damage"]).Calculate(cardPlay.Target);
        await PowerCmd.Apply<BlackCatSealPower>(
            choiceContext,
            cardPlay.Target,
            amount,
            Owner.Creature,
            this);
    }

    private static decimal CountDiaryCards(CardModel card, Creature? _)
    {
        return MainFile.Diary.GetPile(card.Owner).Cards.Count;
    }

    internal static async Task RecallAfterBlackCatSealBloom(ICombatState combatState)
    {
        foreach (var player in combatState.Players)
        {
            LittleDevil[] cards = MainFile.Diary.GetPile(player).Cards
                .OfType<LittleDevil>()
                .Where(card => card.Keywords.Contains(JanusKeywords.Recollection) && !card.CanTake)
                .ToArray();

            foreach (LittleDevil card in cards)
            {
                if (card.Pile?.Type == MainFile.Diary)
                {
                    await card.EnableTake();
                }
            }
        }
    }

    protected override void OnUpgrade() => AddKeyword(JanusKeywords.Recollection);
}

using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interactions.RightClick;

namespace JanusSpire2.JanusSpire2Code.Cards;

public abstract class JanusRecordCardModel : JanusCardModel, IModRightClickableCard
{
    protected bool canTake = false;

    public JanusRecordCardModel(int energyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary = true)
        : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Record, JanusKeywords.Recollection];

    public void EnableTake()
    {
        canTake = true;
    }

    public void DisableTake()
    {
        canTake = false;
    }
    
    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        if (!canTake || Pile?.Type != MainFile.Diary)
        {
            return;
        }

        CardPile hand = PileType.Hand.GetPile(base.Owner);
        if (hand.Cards.Count >= CardPile.MaxCardsInHand)
        {
            return;
        }

        await CardPileCmd.Add(this, PileType.Hand);
        canTake = false;
    }

    protected override CardLocation GetResultLocationForCardPlay()
    {
        CardLocation resultLocationForCardPlay = base.GetResultLocationForCardPlay();
        if (resultLocationForCardPlay.pileType == PileType.Discard)
        {
            resultLocationForCardPlay.pileType = MainFile.Diary;
        }

        return resultLocationForCardPlay;
    }
}
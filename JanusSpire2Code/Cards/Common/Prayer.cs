using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Common;

public sealed class Prayer() : JanusRecordCardModel(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [JanusKeywords.Record, JanusKeywords.Recollection];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUpgraded)
        {
            foreach (CardModel card in PileType.Hand.GetPile(Owner).Cards.Where(card => card.IsUpgradable))
            {
                CardCmd.Upgrade(card);
            }
            return;
        }

        CardModel? selectedCard = await CardSelectCmd.FromHandForUpgrade(choiceContext, Owner, this);
        if (selectedCard is not null)
        {
            CardCmd.Upgrade(selectedCard);
        }
    }

    public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
    {
        if (shuffler == Owner && Pile?.Type == MainFile.Diary)
        {
            await EnableTake();
        }
    }
}

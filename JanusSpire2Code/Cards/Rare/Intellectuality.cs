using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class Intellectuality() : JanusCardModel(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel copy = CreateClone();
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(copy, MainFile.Diary, Owner), 0.2f);
    }

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (Pile?.Type == MainFile.Diary && creator == Owner)
        {
            CardCmd.Upgrade(card);
        }

        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        AddKeyword(JanusKeywords.Collection);
    }
}
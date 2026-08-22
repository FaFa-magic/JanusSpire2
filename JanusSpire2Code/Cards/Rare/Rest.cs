using MegaCrit.Sts2.Core.Entities.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class Rest() : JanusCardModel(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> cardsToRest = PileType.Hand.GetPile(Owner).Cards.ToList();
        if (cardsToRest.Count == 0)
        {
            return;
        }

        foreach (CardModel card in cardsToRest)
        {
            await CardPileCmd.Add(card, MainFile.Diary);
        }

        RestPower? power = await PowerCmd.Apply<RestPower>(
            choiceContext,
            Owner.Creature,
            cardsToRest.Count,
            Owner.Creature,
            this);
        power?.SetCards(cardsToRest);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

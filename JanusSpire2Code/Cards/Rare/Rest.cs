using MegaCrit.Sts2.Core.Entities.Cards;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class Rest() : JanusCardModel(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || CombatManager.Instance.IsOverOrEnding)
            return;

        List<CardModel> cardsToRest = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card != this && card is not JanusRecordMappingCard).ToList();
        if (cardsToRest.Count == 0)
        {
            return;
        }

        // Move the whole snapshot before animations and pile-change hooks run.
        var results = await CardPileCmd.Add(cardsToRest, MainFile.Diary);
        cardsToRest = results.Where(result => result.success).Select(result => result.cardAdded).ToList();
        if (cardsToRest.Count == 0 || CombatManager.Instance.IsOverOrEnding)
            return;

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

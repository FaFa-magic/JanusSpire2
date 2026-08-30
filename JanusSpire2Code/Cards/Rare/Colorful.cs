using System.Collections.Generic;
using System.Threading.Tasks;
using JanusSpire2.JanusSpire2Code.Characters;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class Colorful() : JanusCardModel(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Transcribe];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel copy = CreateClone();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(copy, MainFile.Diary, Owner),
            0.2f);
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (Pile?.Type != MainFile.Diary ||
            card.Owner != Owner ||
            card.Pool == ModelDb.CardPool<JanusCardPool>())
        {
            return playCount;
        }

        return playCount + 1;
    }
}

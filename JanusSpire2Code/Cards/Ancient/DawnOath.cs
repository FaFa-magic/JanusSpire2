using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Ancient;

public sealed class DawnOath() : JanusCardModel(3, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var diaryPile = MainFile.Diary.GetPile(Owner);
        if (diaryPile == null || diaryPile.Cards.Count == 0)
        {
            return;
        }

        List<CardModel> copies = diaryPile.Cards
            .Select(card => card.CreateClone())
            .ToList();

        await CardPileCmd.AddGeneratedCardsToCombat(copies, MainFile.Diary, Owner);
    }
    
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}

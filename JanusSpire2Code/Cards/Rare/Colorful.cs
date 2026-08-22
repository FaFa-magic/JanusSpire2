using JanusSpire2.JanusSpire2Code.Characters;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class Colorful() : JanusCardModel(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Transcribe];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel copy = CreateClone();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(copy, MainFile.Diary, Owner),
            0.2f);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Pile?.Type != MainFile.Diary || !IsUpgraded || Owner.Creature.IsDead || CombatState == null)
        {
            return;
        }

        IEnumerable<CardModel> candidates = Owner.UnlockState.CharacterCardPools
            .Where(pool => pool != ModelDb.CardPool<JanusCardPool>())
            .SelectMany(pool => pool.GetUnlockedCards(
                Owner.UnlockState,
                Owner.RunState.CardMultiplayerConstraint));
        CardModel? generatedCard = CardFactory.GetForCombat(
            Owner,
            candidates,
            1,
            Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();
        if (generatedCard == null)
        {
            return;
        }

        generatedCard.AddKeyword(CardKeyword.Ethereal);
        generatedCard.AddKeyword(JanusKeywords.Record);
        await CardPileCmd.AddGeneratedCardToCombat(generatedCard, PileType.Hand, Owner);
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

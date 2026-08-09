using JanusSpire2.JanusSpire2Code.Characters;
using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace JanusSpire2.JanusSpire2Code.Cards.Ancient;

[RegisterCard(typeof(EventCardPool))]
[RegisterCharacterStarterCard(typeof(JanusCharacter), 1, Order = 5)]
public sealed class AngelPrayer() : JanusRecordCardModel(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, JanusKeywords.Collection];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AngelPrayerPower>(choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);
    }
}
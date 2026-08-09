using JanusSpire2.JanusSpire2Code.Characters;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace JanusSpire2.JanusSpire2Code.Cards.Ancient;

[RegisterCard(typeof(EventCardPool))]
[RegisterCharacterStarterCard(typeof(JanusCharacter), 1, Order = 6)]
public sealed class AngelRest() : JanusRecordCardModel(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, JanusKeywords.Collection];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(3),
        new CardsVar(3)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, base.Owner);
        await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
    }
}
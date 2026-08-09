using JanusSpire2.JanusSpire2Code.Cards.Token;
using JanusSpire2.JanusSpire2Code.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace JanusSpire2.JanusSpire2Code.Cards.Basic;

[RegisterCharacterStarterCard(typeof(JanusCharacter), 1, Order = 3)]
public sealed class CoverUp() : JanusCardModel(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<SmokeEmitter>()];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        
        CardModel card = base.CombatState.CreateCard<SmokeEmitter>(base.Owner);
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, base.Owner);
        }
    }
    
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
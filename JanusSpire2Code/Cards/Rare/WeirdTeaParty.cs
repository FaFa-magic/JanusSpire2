using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Potions;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class WeirdTeaParty() : JanusCardModel(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Collection];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        while (base.Owner.HasOpenPotionSlots)
        {
            if (!(await PotionCmd.TryToProcure<EntropicBrew>(base.Owner)).success)
            {
                break;
            }
        }
        
    }
    
    protected override void OnUpgrade() => base.EnergyCost.UpgradeBy(-1);
}
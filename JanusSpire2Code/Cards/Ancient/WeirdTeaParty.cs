using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Potions;
using STS2RitsuLib.Interop.AutoRegistration;
using JanusSpire2.JanusSpire2Code.Powers;

namespace JanusSpire2.JanusSpire2Code.Cards.Ancient;

[RegisterCard(typeof(EventCardPool))]
public sealed class WeirdTeaParty() : JanusCardModel(2, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        while (base.Owner.HasOpenPotionSlots)
        {
            if (!(await PotionCmd.TryToProcure<EntropicBrew>(base.Owner)).success)
            {
                break;
            }
        }

        await PowerCmd.Apply<WeirdTeaPartyPower>(
            choiceContext,
            Owner.Creature,
            1M,
            Owner.Creature,
            this);
    }
    
    protected override void OnUpgrade() => base.EnergyCost.UpgradeBy(-1);
}

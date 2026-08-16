using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class SmokeGenerator() : JanusCardModel(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ModCardVars.Int("SmokeGenerator", 1)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel card = CreateClone();
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, MainFile.Diary, base.Owner), 0.2f);
    }
    
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(base.Owner.Creature))
        {
            if (base.Owner.Creature.IsDead || this.Pile?.Type != MainFile.Diary)
            {
                return;
            }
            await PowerCmd.Apply<SmokePower>(choiceContext, base.Owner.Creature, base.DynamicVars["SmokeGenerator"].BaseValue, base.Owner.Creature, null);
        }
    }
    
    protected override void OnUpgrade() => base.EnergyCost.UpgradeBy(-1);
}
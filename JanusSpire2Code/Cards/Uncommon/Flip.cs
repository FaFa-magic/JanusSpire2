using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class Flip() : JanusCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ModCardVars.Int("Flip", 2)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel card = CreateClone();
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, MainFile.Diary, base.Owner), 0.2f);
    }
    
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (base.Owner.Creature.IsDead || card.Owner != base.Owner || this.Pile?.Type != MainFile.Diary)
        {
            return;
        }
        CardPile? pile = card.Pile;
        if ((pile != null && pile.Type == MainFile.Diary) || oldPileType == MainFile.Diary)
        {
            await CreatureCmd.TriggerAnim(base.Owner.Creature, "BlockStart", 0.3f);
            await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars["Flip"].BaseValue, ValueProp.Unpowered | ValueProp.Move, null);
        }
    }
    
    protected override void OnUpgrade() => DynamicVars["Flip"].UpgradeValueBy(1M);
}
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Token.Included;

public sealed class IncludedFlip() : JanusCardModel(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Unplayable
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ModCardVars.Int("Flip", 2)
    ];
    
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (base.Owner.Creature.IsDead || card.Owner != base.Owner)
        {
            return;
        }
        CardPile? pile = card.Pile;
        if ((pile != null && pile.Type == MainFile.Diary) || oldPileType == MainFile.Diary)
        {
            await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars["Flip"].BaseValue, ValueProp.Unpowered, null);
        }
    }
    
    protected override void OnUpgrade() => DynamicVars["Flip"].UpgradeValueBy(1M);
}
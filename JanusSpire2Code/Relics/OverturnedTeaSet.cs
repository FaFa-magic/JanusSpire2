using JanusSpire2.JanusSpire2Code.Cards.Ancient;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class OverturnedTeaSet : JanusRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<SpecialParty>();

    public override async Task AfterObtained()
    {
        CardModel card = Owner.RunState.CreateCard<SpecialParty>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
    }
}

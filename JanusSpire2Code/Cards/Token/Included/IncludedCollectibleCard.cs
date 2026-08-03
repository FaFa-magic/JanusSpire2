using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JanusSpire2.JanusSpire2Code.Cards.Token.Included;

public sealed class IncludedCollectibleCard() : JanusCardModel(-1, CardType.Quest, CardRarity.Quest, TargetType.None)
{
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        CardPile? pile = base.Pile;
        if (pile != null && pile.Type == MainFile.Diary && player == base.Owner)
        {
            await CatSticker.CreateInHand(base.Owner, 1, base.Owner.Creature.CombatState, base.IsUpgraded);
        }
    }
}
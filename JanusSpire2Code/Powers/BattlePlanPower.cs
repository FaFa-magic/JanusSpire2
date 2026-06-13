using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class BattlePlanPower : JanusPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player == base.Owner.Player)
        {
            Flash();
            if (CombatManager.Instance.IsOverOrEnding)
            {
                return;
            }
            List<CardModel> fuels = new List<CardModel>();
            for (int i = 0; i < Amount; i++)
            {
                fuels.Add(combatState.CreateCard<Fuel>(this.Owner.Player));
            }
            foreach (var item in fuels)
            {
                CardCmd.Upgrade(item);
                item.AddKeyword(CardKeyword.Retain);
            }
            await CardPileCmd.AddGeneratedCardsToCombat(fuels, PileType.Hand, this.Owner.Player);
        }
    }
}
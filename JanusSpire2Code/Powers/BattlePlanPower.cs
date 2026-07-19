using JanusSpire2.JanusSpire2Code.Cards.Token;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class BattlePlanPower : JanusPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<Plan>(true)];
    
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player == base.Owner.Player)
        {
            Flash();
            if (CombatManager.Instance.IsOverOrEnding)
            {
                return;
            }
            List<CardModel> Plans = new List<CardModel>();
            for (int i = 0; i < Amount; i++)
            {
                Plans.Add(combatState.CreateCard<Plan>(this.Owner.Player));
            }
            foreach (var item in Plans)
            {
                CardCmd.Upgrade(item);
                item.AddKeyword(CardKeyword.Retain);
            }
            await CardPileCmd.AddGeneratedCardsToCombat(Plans, PileType.Hand, this.Owner.Player);
        }
    }
}
using JanusSpire2.JanusSpire2Code.Cards.Token;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace JanusSpire2.JanusSpire2Code.Cards.Common;

public sealed class Runaway() : JanusCardModel(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2),
        new DynamicVar("SwiftAmount", 1M)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<CatSticker>(this.IsUpgraded)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (CardModel item in await CatSticker.CreateInHand(base.Owner, base.DynamicVars.Cards.IntValue, base.Owner.Creature.CombatState, this.IsUpgraded))
        {
            CardCmd.Enchant<Swift>(item, base.DynamicVars["SwiftAmount"].BaseValue);
        }
    }
}
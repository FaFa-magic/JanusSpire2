using JanusSpire2.JanusSpire2Code.Cards.Token.Included;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class Flip() : JanusCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<IncludedFlip>(base.IsUpgraded)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        CardModel card = base.CombatState.CreateCard<IncludedFlip>(base.Owner);
        if (this.IsUpgraded)
        {
            CardCmd.Upgrade(card);
        }
        await CardPileCmd.AddGeneratedCardToCombat(card, MainFile.Diary, base.Owner);
    }
}
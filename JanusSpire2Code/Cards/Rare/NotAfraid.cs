using JanusSpire2.JanusSpire2Code.Cards.Token;
using JanusSpire2.JanusSpire2Code.Tags;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class NotAfraid() : JanusCardModel(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<CatSticker>(base.IsUpgraded)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        IEnumerable<CardModel> enumerable = PileType.Exhaust.GetPile(base.Owner).Cards.Where((CardModel c) => c.Tags.Any(t => t == JanusTags.Scratch)).ToList();
        bool flag = true;
        foreach (CardModel item in enumerable)
        {
            if (base.IsUpgraded)
            {
                CardCmd.Upgrade(item, CardPreviewStyle.None);
            }
            await CardCmd.AutoPlay(choiceContext, item, cardPlay.Target, AutoPlayType.Default, skipXCapture: false, !flag);
            flag = false;
        }
    }
}
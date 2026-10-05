using JanusSpire2.JanusSpire2Code.Cards.Token;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class AfternoonTea() : JanusCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<TeaPartyTime>()];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        TeaPartyTime teaPartyTime = CombatState.CreateCard<TeaPartyTime>(Owner);
        IReadOnlyList<CardPileAddResult> results = await CardPileCmd.AddGeneratedCardsToCombat(
            [teaPartyTime], MainFile.Diary, Owner);
        foreach (CardPileAddResult result in results)
        {
            if (result.success && result.cardAdded is TeaPartyTime generatedCard)
            {
                await generatedCard.EnableTake();
            }
        }

        CardCmd.PreviewCardPileAdd(results, 0.2f);
    }
    
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}

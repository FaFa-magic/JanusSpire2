using JanusSpire2.JanusSpire2Code.Cards.Token;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace JanusSpire2.JanusSpire2Code.Cards.Multiplay;

public sealed class HelpEachOther() : JanusCardModel(3, CardType.Power, CardRarity.Rare, TargetType.AllAllies)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Perk];
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<Assist>()];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        IEnumerable<Creature> enumerable = from c in base.CombatState.GetTeammatesOf(base.Owner.Creature)
            where c != null && c.IsAlive && c.IsPlayer
            select c;

        foreach (Creature teammate in enumerable)
        {
            if (teammate.Player == null)
            {
                continue;
            }

            List<Assist> cards = new List<Assist>();
            for (int i = 0; i < base.DynamicVars.Cards.IntValue; i++)
            {
                cards.Add(base.CombatState.CreateCard<Assist>(teammate.Player));
            }

            IReadOnlyList<CardPileAddResult> results = await CardPileCmd.AddGeneratedCardsToCombat(cards, MainFile.Diary, base.Owner);
            if (LocalContext.IsMe(teammate))
            {
                CardCmd.PreviewCardPileAdd(results);
            }

            await Cmd.Wait(0.1f);
        }
    }
    
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
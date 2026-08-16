using JanusSpire2.JanusSpire2Code.Cards.Token;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class CommemorativeAlbum() : JanusCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2),
        new DynamicVar("BeforeHandDraw", 1)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<CatSticker>(base.IsUpgraded)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CatSticker.CreateInHand(base.Owner, base.DynamicVars.Cards.IntValue, base.Owner.Creature.CombatState, base.IsUpgraded);
        CardModel card = CreateClone();
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, MainFile.Diary, base.Owner), 0.2f);
    }
    
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        CardPile? pile = base.Pile;
        if (pile != null && pile.Type == MainFile.Diary && player == base.Owner)
        {
            await CatSticker.CreateInHand(base.Owner, base.DynamicVars["BeforeHandDraw"].IntValue, base.Owner.Creature.CombatState, base.IsUpgraded);
        }
    }
}
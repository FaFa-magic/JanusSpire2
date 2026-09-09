using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class NewSeason() : JanusCardModel(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Transcribe, CardKeyword.Innate];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [ModCardVars.Int("NewSeason", 2M)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel copy = CreateClone();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(copy, MainFile.Diary, Owner),
            0.2F);
    }

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (Pile?.Type != MainFile.Diary ||
            creator != Owner ||
            Owner.Creature.IsDead)
        {
            return;
        }

        await CreatureCmd.TriggerAnim(Owner.Creature, "BlockStart", 0.3f);
        await CreatureCmd.GainBlock(
            Owner.Creature,
            DynamicVars["NewSeason"].BaseValue,
            ValueProp.Unpowered | ValueProp.Move,
            null);
    }

    protected override void OnUpgrade() => DynamicVars["NewSeason"].UpgradeValueBy(1M);
}

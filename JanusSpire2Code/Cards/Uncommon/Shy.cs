using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class Shy() : JanusCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [ModCardVars.Int("Shy", 3M)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Transcribe];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel card = CreateClone();
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, MainFile.Diary, base.Owner), 0.2f);
    }
    
    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (base.Owner.Creature.IsDead || this.Pile?.Type != MainFile.Diary)
        {
            return;
        }
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "BlockStart", 0.3f);
        await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars["Shy"].BaseValue, ValueProp.Unpowered, null);
    }
    
    protected override void OnUpgrade() => DynamicVars["Shy"].UpgradeValueBy(1M);
}
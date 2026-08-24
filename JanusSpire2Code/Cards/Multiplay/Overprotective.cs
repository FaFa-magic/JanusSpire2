using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Multiplay;

public sealed class Overprotective() : JanusCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ModCardVars.Int("Overprotective", 1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel card = CreateClone();
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(card, MainFile.Diary, base.Owner), 0.2f);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner.Creature ||
            result.UnblockedDamage <= 0 ||
            Pile?.Type != MainFile.Diary ||
            CombatState == null)
        {
            return;
        }

        foreach (Creature ally in CombatState.GetTeammatesOf(Owner.Creature)
                     .Where(creature => creature != Owner.Creature && creature.IsPlayer && creature.IsAlive))
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext,
                ally,
                DynamicVars["Overprotective"].BaseValue,
                Owner.Creature,
                this);
        }
    }
    
    protected override void OnUpgrade() => DynamicVars["Overprotective"].UpgradeValueBy(1M);
}

using JanusSpire2.JanusSpire2Code.Characters;
using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JanusSpire2.JanusSpire2Code.Cards.Ancient;

[RegisterCard(typeof(EventCardPool))]
[RegisterCharacterStarterCard(typeof(JanusCharacter), 1, Order = 5)]
public sealed class AngelPrayer() : JanusRecordCardModel(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    private const string HpLossThresholdKey = "HpLossThreshold";
    private const string HpLostKey = "HpLost";

    private int _hpLostThisCombat;

    public override int MaxUpgradeLevel => 0;

    [SavedProperty]
    public int HpLostThisCombat
    {
        get => _hpLostThisCombat;
        private set
        {
            AssertMutable();
            _hpLostThisCombat = value;
            DynamicVars[HpLostKey].BaseValue = value;
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust, JanusKeywords.Collection, JanusKeywords.Recollection];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(HpLossThresholdKey, 18M),
        new DynamicVar(HpLostKey, HpLostThisCombat)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<AngelPrayerPower>()];

    public override Task BeforeCombatStart()
    {
        if (CombatState != null)
        {
            HpLostThisCombat = 0;
            DisableTake();
        }

        return Task.CompletedTask;
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (CombatState == null || creature != Owner.Creature || delta >= 0M)
        {
            return Task.CompletedTask;
        }

        HpLostThisCombat += decimal.ToInt32(-delta);
        if (Pile?.Type == MainFile.Diary &&
            HpLostThisCombat >= DynamicVars[HpLossThresholdKey].BaseValue)
        {
            EnableTake();
        }

        return Task.CompletedTask;
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AngelPrayerPower>(choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);
    }
}

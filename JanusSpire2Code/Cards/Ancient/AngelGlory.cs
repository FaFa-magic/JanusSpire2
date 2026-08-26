using JanusSpire2.JanusSpire2Code.Characters;
using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards.Ancient;

[RegisterCard(typeof(EventCardPool))]
[RegisterCharacterStarterCard(typeof(JanusCharacter), 1, Order = 4)]
public sealed class AngelGlory() : JanusRecordCardModel(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    private const int BlockGainThreshold = 40;
    private const string BlockGainThresholdKey = "BlockGainThreshold";
    private const string BlockGainedKey = "BlockGained";

    private int _blockGainedThisCombat;

    public override int MaxUpgradeLevel => 0;

    [SavedProperty]
    public int BlockGainedThisCombat
    {
        get => _blockGainedThisCombat;
        private set
        {
            AssertMutable();
            _blockGainedThisCombat = Math.Max(0, value);
            DynamicVars[BlockGainedKey].BaseValue = _blockGainedThisCombat;
            this.RequestVisualReload();
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust, JanusKeywords.Collection, JanusKeywords.Recollection];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(BlockGainThresholdKey, BlockGainThreshold),
        new DynamicVar(BlockGainedKey, BlockGainedThisCombat)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AngelGloryPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block)
    ];

    public override Task BeforeCombatStart()
    {
        if (CombatState != null)
        {
            BlockGainedThisCombat = 0;
            DisableTake();
        }

        return Task.CompletedTask;
    }

    public override Task AfterBlockGained(
        Creature creature,
        decimal amount,
        ValueProp props,
        CardModel? cardSource)
    {
        if (CombatState == null || creature != Owner.Creature || amount <= 0M)
        {
            return Task.CompletedTask;
        }

        BlockGainedThisCombat += decimal.ToInt32(decimal.Floor(amount));
        if (Pile?.Type == MainFile.Diary &&
            BlockGainedThisCombat >= DynamicVars[BlockGainThresholdKey].BaseValue)
        {
            EnableTake();
        }

        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AngelGloryPower>(
            choiceContext,
            Owner.Creature,
            1M,
            Owner.Creature,
            this);
    }
}

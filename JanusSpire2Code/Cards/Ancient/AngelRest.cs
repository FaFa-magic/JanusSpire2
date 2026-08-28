using JanusSpire2.JanusSpire2Code.Characters;
using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards.Ancient;

[RegisterCard(typeof(EventCardPool))]
[RegisterCharacterStarterCard(typeof(JanusCharacter), 1, Order = 6)]
public sealed class AngelRest() : JanusRecordCardModel(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    private const int CardGenerationThreshold = 12;
    private const string CardGenerationThresholdKey = "CardGenerationThreshold";
    private const string CardsGeneratedKey = "CardsGenerated";

    private int _cardsGeneratedThisCombat;

    public override int MaxUpgradeLevel => 0;

    [SavedProperty]
    public int CardsGeneratedThisCombat
    {
        get => _cardsGeneratedThisCombat;
        private set
        {
            AssertMutable();
            _cardsGeneratedThisCombat = Math.Max(0, value);
            DynamicVars[CardsGeneratedKey].BaseValue = _cardsGeneratedThisCombat;
            this.RequestVisualReload();
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust, JanusKeywords.Collection, JanusKeywords.Recollection];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(3),
        new CardsVar(3),
        new DynamicVar(CardGenerationThresholdKey, CardGenerationThreshold),
        new DynamicVar(CardsGeneratedKey, CardsGeneratedThisCombat)
    ];

    public override async Task BeforeCombatStart()
    {
        if (CombatState != null)
        {
            CardsGeneratedThisCombat = 0;
            await DisableTake();
        }
    }

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (CombatState == null || creator != Owner)
        {
            return;
        }

        CardsGeneratedThisCombat++;
        if (Pile?.Type == MainFile.Diary &&
            CardsGeneratedThisCombat >= DynamicVars[CardGenerationThresholdKey].BaseValue)
        {
            await EnableTake();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }
}

using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Powers;
using JanusSpire2.JanusSpire2Code.Tags;
using JanusSpire2.JanusSpire2Code.Cards.Uncommon;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards.Token;

[RegisterCard(typeof(TokenCardPool))]
public sealed class CatSticker() : JanusRecordCardModel(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    private const string BlackCatSealVar = "BlackCatSeal";
    private const decimal MaxDynamicVarValue = 999999999M;

    public override int MaxUpgradeLevel => 999;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://JanusSpire2/images/cards/{GetType().Name}.png");

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust, JanusKeywords.Sticker, JanusKeywords.Recollection];
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<BlackCatSealPower>()];
    
    protected override HashSet<CardTag> CanonicalTags => [
        JanusTags.Scratch
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(1M, ValueProp.Move),
        new CalculationBaseVar(1M),
        new CalculationExtraVar(1M),
        new CalculatedVar(BlackCatSealVar).WithMultiplier(CalculateConcealGatheringsBonus)
    ];
    
    public static async Task<IEnumerable<CatSticker>> CreateInDiary(Player owner, int amount, ICombatState? combatState, bool isUpgraded)
    {
        IEnumerable<CatSticker> scratchs = Create(owner, amount, combatState, isUpgraded);
        await CardPileCmd.AddGeneratedCardsToCombat(scratchs, MainFile.Diary, owner);
        return scratchs;
    }
    
    public static async Task<IEnumerable<CatSticker>> CreateInHand(Player owner, int amount, ICombatState? combatState, bool isUpgraded)
    {
        IEnumerable<CatSticker> scratchs = Create(owner, amount, combatState, isUpgraded);
        await CardPileCmd.AddGeneratedCardsToCombat(scratchs, PileType.Hand, owner);
        return scratchs;
    }

    public static IEnumerable<CatSticker> Create(Player owner, int amount, ICombatState? combatState, bool isUpgraded)
    {
        List<CatSticker> list = new List<CatSticker>();
        if (combatState != null)
        {
            for (int i = 0; i < amount; i++)
            {
                list.Add(combatState.CreateCard<CatSticker>(owner));
            }
            if (isUpgraded)
            {
                foreach (var item in list)
                {
                    CardCmd.Upgrade(item);
                }
            }
        }
        return list;
    }

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (card == this && Pile?.Type == MainFile.Diary)
        {
            EnableTake();
        }

        return Task.CompletedTask;
    }

    public override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        if (card.Owner == Owner &&
            card is ConcealGatherings &&
            (oldPileType == MainFile.Diary || card.Pile?.Type == MainFile.Diary))
        {
            this.RequestVisualReload();
        }

        return Task.CompletedTask;
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        
        decimal blackCatSeal = ((CalculatedVar)DynamicVars[BlackCatSealVar]).Calculate(cardPlay.Target);
        await PowerCmd.Apply<BlackCatSealPower>(choiceContext, cardPlay.Target, blackCatSeal, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVar blackCatSeal = DynamicVars.CalculationBase;
        decimal targetValue = GetBlackCatSealForUpgradeLevel(CurrentUpgradeLevel);
        blackCatSeal.UpgradeValueBy(targetValue - blackCatSeal.BaseValue);
    }

    private static decimal CalculateConcealGatheringsBonus(CardModel card, Creature? target)
    {
        return MainFile.Diary.GetPile(card.Owner).Cards
            .OfType<ConcealGatherings>()
            .Sum(concealGatherings => concealGatherings.DynamicVars[BlackCatSealVar].BaseValue);
    }

    private static decimal GetBlackCatSealForUpgradeLevel(int upgradeLevel)
    {
        if (upgradeLevel <= 0)
        {
            return 1M;
        }

        decimal previous = 1M;
        decimal current = 2M;
        for (int level = 1; level < upgradeLevel; level++)
        {
            decimal next = Math.Min(previous + current, MaxDynamicVarValue);
            previous = current;
            current = next;
        }

        return current;
    }
}

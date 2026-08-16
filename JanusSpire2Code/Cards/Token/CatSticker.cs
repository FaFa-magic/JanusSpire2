using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Powers;
using JanusSpire2.JanusSpire2Code.Tags;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Token;

public sealed class CatSticker() : JanusTokenCardModel(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, JanusKeywords.Sticker];
    
    protected override HashSet<CardTag> CanonicalTags => [
        JanusTags.Scratch
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(1M, ValueProp.Move),
        ModCardVars.Int("BlackCatSeal", 1)
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
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        
        await PowerCmd.Apply<BlackCatSealPower>(choiceContext, cardPlay.Target, DynamicVars["BlackCatSeal"].BaseValue, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade() => DynamicVars["BlackCatSeal"].UpgradeValueBy(1M);
}
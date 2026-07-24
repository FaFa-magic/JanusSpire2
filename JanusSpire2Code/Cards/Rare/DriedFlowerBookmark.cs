using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class DriedFlowerBookmark() : JanusCardModel(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override int MaxUpgradeLevel => 999;
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, JanusKeywords.Sticker];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10M, ValueProp.Move)];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
    
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3M);
}
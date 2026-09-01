using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Cards.Token;

[RegisterCard(typeof(TokenCardPool))]
public sealed class SmokeEmitter() : JanusRecordCardModel(0, CardType.Skill, CardRarity.Token, TargetType.AnyPlayer)
{
    public override int MaxUpgradeLevel => 999;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Exhaust, JanusKeywords.Sticker, JanusKeywords.Recollection];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ModCardVars.Int("Smoke", 1)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Creature target = cardPlay.Target ?? Owner.Creature;
        await PowerCmd.Apply<SmokePower>(
            choiceContext,
            target,
            DynamicVars["Smoke"].BaseValue,
            Owner.Creature,
            this);
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (amount < 0M &&
            power is SmokePower { Amount: <= 0 } &&
            power.Owner == Owner.Creature &&
            Pile?.Type == MainFile.Diary &&
            !CanTake)
        {
            await EnableTake();
        }
    }
    
    protected override void OnUpgrade() => DynamicVars["Smoke"].UpgradeValueBy(1M);
}

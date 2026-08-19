using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class LittleCute() : JanusCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ModCardVars.Int("LittleCute", 1),
        ModCardVars.Int("BlackCatSeal", 4)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));
        await PowerCmd.Apply<BlackCatSealPower>(choiceContext, cardPlay.Target, DynamicVars["BlackCatSeal"].BaseValue, base.Owner.Creature, this);
    }
    
    protected override void OnUpgrade() => base.EnergyCost.UpgradeBy(-1);
}
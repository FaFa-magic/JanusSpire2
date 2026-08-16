using JanusSpire2.JanusSpire2Code.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class LonelyNight() : JanusCardModel(2, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = base.Owner?.Creature?.CombatState;
        if (combatState == null)
        {
            return;
        }

        int multiplier = this.IsUpgraded ? 2 : 1;

        foreach (var enemy in combatState.HittableEnemies)
        {
            if (enemy != null && enemy.IsAlive)
            {
                int num = enemy.GetPowerAmount<BlackCatSealPower>();
                if (num > 0)
                {
                    await PowerCmd.Apply<BlackCatSealPower>(choiceContext, enemy, num * multiplier, base.Owner?.Creature, this);
                }
            }
        }
    }
}
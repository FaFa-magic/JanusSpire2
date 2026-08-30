using JanusSpire2.JanusSpire2Code.Cards.Token;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class SmokeGenerator() : JanusCardModel(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromCard<SmokeEmitter>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel card = CreateClone();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(card, MainFile.Diary, Owner),
            0.2F);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner.Creature) ||
            Owner.Creature.IsDead ||
            Pile?.Type != MainFile.Diary)
        {
            return;
        }

        ICombatState combatState = CombatState
            ?? throw new InvalidOperationException("SmokeGenerator must be in combat to play SmokeEmitter.");
        CardModel smokeEmitter = combatState.CreateCard<SmokeEmitter>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(smokeEmitter, PileType.Play, Owner);
        await CardCmd.AutoPlay(choiceContext, smokeEmitter, Owner.Creature);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

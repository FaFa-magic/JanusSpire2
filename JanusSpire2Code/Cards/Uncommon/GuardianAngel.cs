using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class GuardianAngel() : JanusCardModel(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Transcribe];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel copy = CreateClone();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(copy, MainFile.Diary, Owner),
            0.2F);
    }

    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (Pile?.Type != MainFile.Diary ||
            card.Owner != Owner ||
            !card.Keywords.Contains(JanusKeywords.Counterattack))
        {
            return false;
        }

        modifiedCost = originalCost + 1M;
        return true;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Pile?.Type != MainFile.Diary ||
            Owner.Creature.IsDead ||
            !participants.Contains(Owner.Creature))
        {
            return;
        }

        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            0M,
            ValueProp.Unblockable | ValueProp.Unpowered,
            Owner.Creature,
            this,
            null);
    }

    protected override void OnUpgrade() => AddKeyword(JanusKeywords.Counterattack);
}

using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Enchantment;

[RegisterEnchantment]
public sealed class HallucinationEnchantment : ModEnchantmentTemplate
{
    public override bool ShowAmount => false;

    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://icon.svg"
    );
    
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == this.Card.Owner.Creature.Side && combatState.RoundNumber <= 1 && this.Card.Owner.Creature.Player != null)
        {
            CardModel? cardModel = CardFactory.GetDistinctForCombat(this.Card.Owner.Creature.Player, this.Card.Owner.Creature.Player.Character.CardPool.GetUnlockedCards(this.Card.Owner.Creature.Player.UnlockState, this.Card.Owner.Creature.Player.RunState.CardMultiplayerConstraint), 1, this.Card.Owner.Creature.Player.RunState.Rng.CombatCardGeneration).FirstOrDefault();
            if (cardModel != null)
            {
                await CardCmd.Transform(this.Card, cardModel);
            }
        }
    }
}
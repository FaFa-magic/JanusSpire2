using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
    
    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Card.Owner.Creature.Side || combatState.RoundNumber > 1)
        {
            return;
        }

        var player = Card.Owner;
        CardModel? replacement = CardFactory.GetDistinctForCombat(
                player,
                player.Character.CardPool.GetUnlockedCards(
                    player.UnlockState,
                    player.RunState.CardMultiplayerConstraint),
                1,
                player.RunState.Rng.CombatCardGeneration)
            .FirstOrDefault();
        if (replacement == null)
        {
            return;
        }

        CardPileAddResult? transformResult = await CardCmd.Transform(Card, replacement);
        if (transformResult is { } result && result.success)
        {
            CardCmd.Upgrade(result.cardAdded);
        }
    }
}

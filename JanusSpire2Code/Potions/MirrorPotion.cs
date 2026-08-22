namespace JanusSpire2.JanusSpire2Code.Potions;

using System.Linq;
using System.Threading.Tasks;
using System;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

public sealed class MirrorPotion : JanusPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);

        Player player = target.Player ??
            throw new InvalidOperationException("Mirror Potion must target a player.");
        CardModel? selectedCard = (await CardSelectCmd.FromHand(
            choiceContext,
            player,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            null,
            this)).FirstOrDefault();

        if (selectedCard is null)
        {
            return;
        }

        CardModel copy = selectedCard.CreateClone();
        copy.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
    }
}

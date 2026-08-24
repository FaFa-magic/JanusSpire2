using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;

namespace JanusSpire2.JanusSpire2Code.Scripts;

internal static class HandEnchantmentSelectCmd
{
    private const string DeckEnchantSelectScene =
        "screens/card_selection/deck_enchant_select_screen";

    public static async Task<IEnumerable<CardModel>> FromHand(
        PlayerChoiceContext choiceContext,
        Player player,
        EnchantmentModel enchantment,
        int amount,
        CardSelectorPrefs prefs,
        Func<CardModel, bool>? additionalFilter,
        AbstractModel source)
    {
        Func<CardModel, bool> filter = card =>
            enchantment.CanEnchant(card) && (additionalFilter?.Invoke(card) ?? true);

        Control? description = TryShowEnchantmentDescription(
            player,
            enchantment,
            amount,
            prefs,
            filter);
        try
        {
            return await CardSelectCmd.FromHand(
                choiceContext,
                player,
                prefs,
                filter,
                source);
        }
        finally
        {
            if (description != null && GodotObject.IsInstanceValid(description))
            {
                description.QueueFree();
            }
        }
    }

    private static Control? TryShowEnchantmentDescription(
        Player player,
        EnchantmentModel canonicalEnchantment,
        int amount,
        CardSelectorPrefs prefs,
        Func<CardModel, bool> filter)
    {
        if (!LocalContext.IsMe(player) ||
            RunManager.Instance.NetService.Type == NetGameType.Replay ||
            PileType.Hand.GetPile(player).Cards.Count(filter) <= prefs.MinSelect ||
            NPlayerHand.Instance is not { } hand)
        {
            return null;
        }

        Control template = PreloadManager.Cache
            .GetScene(SceneHelper.GetScenePath(DeckEnchantSelectScene))
            .Instantiate<Control>(PackedScene.GenEditState.Disabled);
        Control description = template.GetNode<Control>("%EnchantmentDescriptionContainer");
        template.RemoveChild(description);
        template.Free();

        EnchantmentModel enchantment = canonicalEnchantment.ToMutable();
        enchantment.Amount = amount;
        enchantment.RecalculateValues();

        description.Name = "JanusHandEnchantmentDescription";
        description.GetNode<TextureRect>(
            "MarginContainer/HBoxContainer/EnchantmentIcon").Texture = enchantment.Icon;
        description.GetNode<MegaLabel>(
                "MarginContainer/HBoxContainer/VBoxContainer/EnchantmentTitle")
            .SetTextAutoSize(enchantment.Title.GetFormattedText());
        description.GetNode<MegaRichTextLabel>(
                "MarginContainer/HBoxContainer/VBoxContainer/EnchantmentDescription").Text =
            enchantment.DynamicDescription.GetFormattedText();
        IgnoreMouseInput(description);

        hand.AddChild(description);
        return description;
    }

    private static void IgnoreMouseInput(Control control)
    {
        control.MouseFilter = Control.MouseFilterEnum.Ignore;
        foreach (Node child in control.GetChildren())
        {
            if (child is Control childControl)
            {
                IgnoreMouseInput(childControl);
            }
        }
    }
}

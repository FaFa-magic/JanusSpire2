using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class MagicGloves : JanusRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.Transform)
    ];

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        List<CardModel> selectedCards = (await CardSelectCmd.FromDeckForTransformation(
            Owner,
            new CardSelectorPrefs(
                CardSelectorPrefs.TransformSelectionPrompt,
                DynamicVars.Cards.IntValue)
            {
                Cancelable = false
            })).ToList();

        if (selectedCards.Count == 0)
        {
            return;
        }

        Flash();
        foreach (CardModel card in selectedCards)
        {
            await CardCmd.TransformToRandom(card, Owner.RunState.Rng.Niche);
        }
    }
}

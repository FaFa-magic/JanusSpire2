using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using JanusSpire2.JanusSpire2Code.Scripts;

namespace JanusSpire2.JanusSpire2Code.Cards.Common;

public sealed class Hesitate() : JanusCardModel(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(8M, ValueProp.Move),
        new DynamicVar("SwiftAmount", 1M)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        Swift swift = ModelDb.Enchantment<Swift>();
        CardModel? card = (await HandEnchantmentSelectCmd.FromHand(
            choiceContext,
            Owner,
            swift,
            DynamicVars["SwiftAmount"].IntValue,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1),
            null,
            this)).FirstOrDefault();
        if (card == null) return;

        CardCmd.Enchant<Swift>(card, DynamicVars["SwiftAmount"].BaseValue);

        NCardEnchantVfx? vfx = NCardEnchantVfx.Create(card);
        if (vfx != null) NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2M);
        DynamicVars["SwiftAmount"].UpgradeValueBy(1M);
    }
}

using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace JanusSpire2.JanusSpire2Code.Cards.Status;

[RegisterCard(typeof(TokenCardPool))]
public sealed class Wave() : JanusCardModel(1, CardType.Status, CardRarity.Status, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        ModCardVars.Int("Wave", 3)
    ];
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [JanusKeywords.Record];
    
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card == this)
        {
            await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars["Wave"].BaseValue, ValueProp.Unpowered | ValueProp.Move, null);
        }
    }
    
    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
    }
}
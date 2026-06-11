using JanusSpire2.JanusSpire2Code.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Relics;

[RegisterCard(typeof(JanusRelicPool))]
[RegisterCharacterStarterRelic(typeof(JanusCharacter))]
public sealed class Coronet : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://JanusSpire2/images/relics/packed/{GetType().Name}.png",
        IconOutlinePath: $"res://JanusSpire2/images/relics/outline/{GetType().Name}.png",
        BigIconPath: $"res://JanusSpire2/images/relics/big/{GetType().Name}.png"
    );
    
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, player);
    }
}
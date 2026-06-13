using JanusSpire2.JanusSpire2Code.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Relics;

[RegisterRelic(typeof(JanusRelicPool), Inherit = true)]
public abstract class JanusRelicModel : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://JanusSpire2/images/relics/packed/{GetType().Name}.png",
        IconOutlinePath: $"res://JanusSpire2/images/relics/outline/{GetType().Name}.png",
        BigIconPath: $"res://JanusSpire2/images/relics/big/{GetType().Name}.png"
    );
}
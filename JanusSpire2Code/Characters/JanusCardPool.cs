using Godot;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace JanusSpire2.JanusSpire2Code.Characters;

public sealed class JanusCardPool : TypeListCardPoolModel, IModColorfulPhilosophersCardPool
{
    public override string Title => JanusCharacter.CharacterId;
    public override string EnergyColorName => JanusCharacter.CharacterId;
    
    public override string BigEnergyIconPath => 
        "res://JanusSpire2/images/packed/sprite_fonts/Janus_energy_icon_original.png";
    public override string TextEnergyIconPath => 
        "res://JanusSpire2/images/packed/sprite_fonts/Janus_energy_icon.png";
    
    public override Color DeckEntryCardColor => new("7BABC2");
    public override Color EnergyOutlineColor => new("7BABC2");
    
    private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateUnmodulatedHsvShaderMaterial();
    public override Material? PoolFrameMaterial => _poolFrameMaterial;
    
    public override bool IsColorless => false;
}
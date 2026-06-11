using Godot;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace JanusSpire2.JanusSpire2Code.Characters;

public sealed class JanusCardPool : TypeListCardPoolModel
{
    public override string Title => JanusCharacter.CharacterId;
    public override string EnergyColorName => JanusCharacter.CharacterColor;
    
    public override string BigEnergyIconPath => 
        "res://JanusSpire2/images/packed/sprite_fonts/Janus_energy_icon_original.png";
    public override string TextEnergyIconPath => 
        "res://JanusSpire2/images/packed/sprite_fonts/Janus_energy_icon.png";
    
    public override Color DeckEntryCardColor => new("FFB2FF");
    public override Color EnergyOutlineColor => new("FFB2FF");
    
    private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateReplaceHueShaderMaterial(0.5f, 0.5f, 1f);
    public override Material? PoolFrameMaterial => _poolFrameMaterial;
    
    public override bool IsColorless => false;
}
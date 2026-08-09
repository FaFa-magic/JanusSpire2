using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Characters;

public sealed class JanusPotionPool  : TypeListPotionPoolModel
{
    public override string EnergyColorName => JanusCharacter.CharacterId;
    
    public override string BigEnergyIconPath => 
        "res://JanusSpire2/images/packed/sprite_fonts/Janus_energy_icon_original.png";
    public override string TextEnergyIconPath => 
        "res://JanusSpire2/images/packed/sprite_fonts/Janus_energy_icon.png";
}
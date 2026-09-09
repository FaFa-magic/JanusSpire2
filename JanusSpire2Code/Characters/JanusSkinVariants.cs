using STS2RitsuLib.Interop.AutoRegistration;

namespace JanusSpire2.JanusSpire2Code.Characters;

public abstract class JanusSkinVariant : JanusCharacter
{
	public sealed override bool HideFromVanillaCharacterSelect => true;
	public sealed override bool AllowInVanillaRandomCharacterSelect => false;
	public sealed override bool HideInCardLibraryCompendium => true;
}

[RegisterCharacter]
public sealed class JanusVariantThree : JanusSkinVariant
{
	public override JanusSkin CurrentSkin => JanusSkin.VariantThree;
}

[RegisterCharacter]
public sealed class JanusVariantFour : JanusSkinVariant
{
	public override JanusSkin CurrentSkin => JanusSkin.VariantFour;
}

[RegisterCharacter]
public sealed class JanusVariantFive : JanusSkinVariant
{
	public override JanusSkin CurrentSkin => JanusSkin.VariantFive;
}

[RegisterCharacter]
public sealed class JanusVariantSix : JanusSkinVariant
{
	public override JanusSkin CurrentSkin => JanusSkin.VariantSix;
}

[RegisterCharacter]
public sealed class JanusVariantSeven : JanusSkinVariant
{
	public override JanusSkin CurrentSkin => JanusSkin.VariantSeven;
}

[RegisterCharacter]
public sealed class JanusVariantH : JanusSkinVariant
{
	public override JanusSkin CurrentSkin => JanusSkin.VariantH;
}

using MegaCrit.Sts2.Core.Entities.Cards;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class FlowerCrossing() : JanusCardModel(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;
}
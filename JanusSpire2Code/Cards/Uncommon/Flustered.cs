using MegaCrit.Sts2.Core.Entities.Cards;

namespace JanusSpire2.JanusSpire2Code.Cards.Uncommon;

public sealed class Flustered() : JanusCardModel(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override bool HasEnergyCostX => true;
}
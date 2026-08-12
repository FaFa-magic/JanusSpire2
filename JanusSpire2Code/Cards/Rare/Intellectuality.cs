using JanusSpire2.JanusSpire2Code.Keywords;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace JanusSpire2.JanusSpire2Code.Cards.Rare;

public sealed class Intellectuality() : JanusCardModel(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override void OnUpgrade()
    {
        AddKeyword(JanusKeywords.Collection);
    }
}
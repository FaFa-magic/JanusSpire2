using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;

namespace JanusSpire2.JanusSpire2Code.Cards.Status;

[RegisterCard(typeof(TokenCardPool))]
public sealed class Wave() : JanusCardModel(1, CardType.Status, CardRarity.Status, TargetType.None)
{
    
}
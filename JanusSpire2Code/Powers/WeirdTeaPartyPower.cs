using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace JanusSpire2.JanusSpire2Code.Powers;

public sealed class WeirdTeaPartyPower : JanusPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        Player? player = Owner.Player;
        if (player == null)
        {
            return;
        }

        foreach (PotionModel potion in player.PotionSlots.OfType<PotionModel>().ToList())
        {
            await PotionCmd.Discard(potion);
        }
    }
}

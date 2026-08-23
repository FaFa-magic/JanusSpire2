using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class RelicFragment : JanusRelicModel
{
    private const int RequiredFragments = 3;

    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool HasUponPickupEffect => true;

    public override RelicAssetProfile AssetProfile =>
        ContentAssetProfiles.Relic("CrackedCore");

    public override async Task AfterRewardTaken(Player player, Reward reward)
    {
        if (player != Owner ||
            reward is not RelicReward relicReward ||
            !ReferenceEquals(relicReward.ClaimedRelic, this))
        {
            return;
        }

        List<RelicFragment> fragments = Owner.Relics
            .OfType<RelicFragment>()
            .ToList();
        if (fragments.Count < RequiredFragments)
        {
            return;
        }

        foreach (RelicFragment fragment in fragments)
        {
            await RelicCmd.Remove(fragment);
        }

        RelicModel randomRelic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(randomRelic, Owner);
    }
}

using JanusSpire2.JanusSpire2Code.Keywords;
using JanusSpire2.JanusSpire2Code.RestSite;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Runs;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class BookAndPen : JanusRelicModel
{
    internal const string WriteDiaryOptionId = "JANUS_WRITE_DIARY";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(JanusKeywords.Collection)
    ];

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner)
        {
            return false;
        }

        options.Add(new WriteDiaryRestSiteOption(player, this));
        return true;
    }

    public override bool ShouldDisableRemainingRestSiteOptions(Player player)
    {
        if (player != Owner)
        {
            return true;
        }

        var synchronizer = RunManager.Instance.RestSiteSynchronizer;
        int? chosenIndex = synchronizer.GetChosenOptionIndex(player.NetId);
        IReadOnlyList<RestSiteOption> options = synchronizer.GetOptionsForPlayer(player);
        return chosenIndex is not int index ||
               index < 0 ||
               index >= options.Count ||
               options[index].OptionId != WriteDiaryOptionId;
    }

    internal void OnDiaryWritten()
    {
        Flash();
    }
}

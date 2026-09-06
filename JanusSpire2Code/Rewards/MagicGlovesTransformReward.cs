using JanusSpire2.JanusSpire2Code.Relics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using STS2RitsuLib.Combat.Rewards;

namespace JanusSpire2.JanusSpire2Code.Rewards;

public sealed class MagicGlovesTransformReward(Player player) : ModCustomReward(player)
{
    private static RewardType? _rewardType;

    public override RewardType ModRewardType => _rewardType
        ?? throw new InvalidOperationException("Magic Gloves reward has not been registered.");

    protected override string DescriptionLocTable => "relics";

    protected override string DescriptionLocKey =>
        "JANUS_SPIRE2_RELIC_MAGIC_GLOVES.rewardDescription";

    protected override string RewardIconPath =>
        "res://JanusSpire2/images/packed/rewards/magic_gloves_reward.png";

    internal static void Register()
    {
        _rewardType = ModRewardRegistry.For(MainFile.ModId)
            .RegisterOwned(
                "magic_gloves_transform",
                (_, owner, _) => new MagicGlovesTransformReward(owner))
            .RewardType;
    }

    protected override async Task<bool> OnSelect()
    {
        CardSelectorPrefs prefs = new(
            CardSelectorPrefs.TransformSelectionPrompt,
            MagicGloves.CardCount)
        {
            Cancelable = false
        };

        List<CardModel> selectedCards =
            (await CardSelectCmd.FromDeckForTransformation(Player, prefs)).ToList();

        Player.GetRelic<MagicGloves>()?.Flash();
        foreach (CardModel card in selectedCards)
        {
            await CardCmd.TransformToRandom(card, Player.RunState.Rng.Niche);
        }

        return true;
    }

    public override void MarkContentAsSeen()
    {
    }
}

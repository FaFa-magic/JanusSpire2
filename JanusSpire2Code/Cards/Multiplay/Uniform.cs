using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace JanusSpire2.JanusSpire2Code.Cards.Multiplay;

public sealed class Uniform() : JanusCardModel(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10M, ValueProp.Move),
        new DynamicVar("SwiftAmount", 1M)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<Swift>(DynamicVars["SwiftAmount"].IntValue);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (CombatState == null || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        Swift swift = ModelDb.Enchantment<Swift>();
        ulong localPlayerId = LocalContext.NetId ?? Owner.NetId;
        List<Player> players = CombatState.Players
            .Where(player => player.Creature.IsAlive &&
                             PileType.Hand.GetPile(player).Cards.Any(swift.CanEnchant))
            .OrderBy(player => player == Owner)
            .ToList();

        foreach (Player player in players)
        {
            BranchingPlayerChoiceContext playerChoiceContext = new(
                this,
                localPlayerId,
                GameActionType.Combat,
                choiceContext);
            Task enchantTask = SelectAndEnchant(playerChoiceContext, player, swift);
            await playerChoiceContext.AssignTaskAndWaitForPauseOrCompletion(enchantTask);
        }
    }

    private async Task SelectAndEnchant(
        PlayerChoiceContext choiceContext,
        Player player,
        Swift swift)
    {
        CardModel? selected = (await CardSelectCmd.FromHand(
            choiceContext,
            player,
            new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1),
            swift.CanEnchant,
            this)).FirstOrDefault();
        if (selected == null ||
            CombatManager.Instance.IsOverOrEnding ||
            player.Creature.IsDead ||
            !swift.CanEnchant(selected))
        {
            return;
        }

        CardCmd.Enchant<Swift>(selected, DynamicVars["SwiftAmount"].BaseValue);
        NCardEnchantVfx? vfx = NCardEnchantVfx.Create(selected);
        if (vfx != null)
        {
            NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
        }
    }

    protected override void OnUpgrade() => DynamicVars["SwiftAmount"].UpgradeValueBy(1M);
}

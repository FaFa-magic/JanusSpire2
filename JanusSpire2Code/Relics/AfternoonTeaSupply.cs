using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace JanusSpire2.JanusSpire2Code.Relics;

public sealed class AfternoonTeaSupply : JanusRelicModel
{
    private const string StrengthVar = "StrengthPower";

    private static int _forcedTargetSelectionDepth;

    internal static bool IsForcingPotionTargetSelection => _forcedTargetSelectionDepth > 0;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(StrengthVar, 1M)
    ];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            -DynamicVars[StrengthVar].BaseValue,
            Owner.Creature,
            null);
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature) ||
            !Owner.HasOpenPotionSlots ||
            CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        PotionModel potion = PotionFactory.CreateRandomPotionInCombat(
            Owner,
            Owner.RunState.Rng.CombatPotionGeneration).ToMutable();
        PotionProcureResult result = await PotionCmd.TryToProcure(potion, Owner);
        if (!result.success)
        {
            return;
        }

        Flash();
        Creature? target = GetAutomaticTarget(potion);
        if (RequiresTargetSelection(potion))
        {
            target = await SelectTarget(choiceContext, potion, combatState);
        }

        if (!potion.IsValidTarget(target))
        {
            await PotionCmd.Discard(potion);
            return;
        }

        if (LocalContext.IsMe(Owner))
        {
            potion.EnqueueManualUse(target);
        }
    }

    private static bool RequiresTargetSelection(PotionModel potion)
    {
        return potion.TargetType is TargetType.AnyEnemy or TargetType.AnyAlly ||
               potion.TargetType == TargetType.AnyPlayer && potion.Owner.RunState.Players.Count > 1;
    }

    private static Creature? GetAutomaticTarget(PotionModel potion)
    {
        return potion.TargetType is TargetType.Self or TargetType.AnyPlayer
            ? potion.Owner.Creature
            : null;
    }

    private async Task<Creature?> SelectTarget(
        PlayerChoiceContext choiceContext,
        PotionModel potion,
        ICombatState combatState)
    {
        List<Creature> creatures = combatState.Creatures.ToList();
        if (!creatures.Any(potion.IsValidTarget))
        {
            return null;
        }

        PlayerChoiceSynchronizer synchronizer = RunManager.Instance.PlayerChoiceSynchronizer;
        uint choiceId = synchronizer.ReserveChoiceId(Owner);
        await choiceContext.SignalPlayerChoiceBegun(Owner, PlayerChoiceOptions.CancelPlayCardActions);

        Creature? target;
        if (LocalContext.IsMe(Owner))
        {
            target = await SelectLocalTarget(potion, combatState);
            int targetIndex = target == null ? -1 : creatures.IndexOf(target);
            synchronizer.SyncLocalChoice(Owner, choiceId, PlayerChoiceResult.FromIndex(targetIndex));
        }
        else
        {
            int targetIndex = (await synchronizer.WaitForRemoteChoice(Owner, choiceId)).AsIndex();
            target = targetIndex >= 0 && targetIndex < creatures.Count
                ? creatures[targetIndex]
                : null;
        }

        await choiceContext.SignalPlayerChoiceEnded();
        return target != null && potion.IsValidTarget(target) ? target : null;
    }

    private static async Task<Creature?> SelectLocalTarget(PotionModel potion, ICombatState combatState)
    {
        while (!CombatManager.Instance.IsOverOrEnding && !potion.HasBeenRemovedFromState)
        {
            List<Creature> validTargets = combatState.Creatures.Where(potion.IsValidTarget).ToList();
            if (validTargets.Count == 0)
            {
                return null;
            }

            NTargetManager targetManager = NTargetManager.Instance;
            bool usingController = NControllerManager.Instance!.IsUsingDirectionalNavigation;
            TargetMode targetMode = usingController
                ? TargetMode.Controller
                : TargetMode.ClickMouseToTarget;

            _forcedTargetSelectionDepth++;
            try
            {
                NCombatRoom combatRoom = NCombatRoom.Instance!;
                targetManager.StartTargeting(
                    potion.TargetType,
                    NRun.Instance!.GlobalUi.TopBar.PotionContainer,
                    targetMode,
                    () => CombatManager.Instance.IsOverOrEnding || potion.HasBeenRemovedFromState,
                    null);

                if (usingController)
                {
                    List<Control> targetHitboxes = validTargets
                        .Select(target => combatRoom.GetCreatureNode(target)?.Hitbox)
                        .OfType<Control>()
                        .ToList();
                    combatRoom.RestrictControllerNavigation(targetHitboxes);
                    targetHitboxes.FirstOrDefault()?.TryGrabFocus();
                }

                Node? selectedNode = await targetManager.SelectionFinished();
                Creature? selectedTarget = selectedNode switch
                {
                    NCreature creatureNode => creatureNode.Entity,
                    NMultiplayerPlayerState playerState => playerState.Player.Creature,
                    _ => null
                };

                if (selectedTarget != null && potion.IsValidTarget(selectedTarget))
                {
                    return selectedTarget;
                }
            }
            finally
            {
                NCombatRoom.Instance?.EnableControllerNavigation();
                _forcedTargetSelectionDepth--;
            }
        }

        return null;
    }
}

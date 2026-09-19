using Godot;
using JanusSpire2.JanusSpire2Code.Nodes;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Powers;

/// <summary>
/// Hidden combat state for Ianus Form's persistent flower VFX.
/// The gameplay card remains unchanged; this power only owns and triggers presentation.
/// </summary>
public sealed class IanvsFormVfxPower : JanusPowerModel
{
    private NIanvsFormVfx? _vfx;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldPlayVfx => false;
    public override PowerAssetProfile AssetProfile => PowerAssetProfile.Empty;

    protected override bool IsVisibleInternal => false;

    private NIanvsFormVfx? Vfx
    {
        get => _vfx is not null && GodotObject.IsInstanceValid(_vfx) ? _vfx : null;
        set
        {
            AssertMutable();
            _vfx = value;
        }
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        Vfx = NIanvsFormVfx.Create(Owner);
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        // Form holders intentionally display only one form at a time. If another form
        // replaced this VFX, playing Ianus Form again makes its flowers current again.
        if (power == this && Vfx is null)
        {
            Vfx = NIanvsFormVfx.Create(Owner);
        }

        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        Vfx?.SetActive(false);
        return Task.CompletedTask;
    }

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator == Owner.Player && !Owner.IsDead)
        {
            Vfx?.GrowFlower();
        }

        return Task.CompletedTask;
    }
}

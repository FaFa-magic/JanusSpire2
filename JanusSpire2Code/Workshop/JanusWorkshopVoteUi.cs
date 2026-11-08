using Godot;
using JanusSpire2.JanusSpire2Code.Nodes;
using JanusSpire2.JanusSpire2Code.Patches;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;

namespace JanusSpire2.JanusSpire2Code.Workshop;

internal static class JanusWorkshopVoteUi
{
    private const string ButtonName = "JanusWorkshopVoteButton";
    private const string ScenePath = "res://JanusSpire2/scenes/ui/janus_workshop_vote_button.tscn";

    public static void Register(ModPatcher patcher)
    {
        if (!MainFile.EnableWorkshopVote)
            return;

        ModNodeAttachmentRegistry.For(MainFile.ModId)
            .RegisterReadyChildFromScene<NCharacterSelectScreen, JanusWorkshopVoteButton>(
                "workshop_vote", ScenePath,
                options: new NodeAttachmentOptions
                {
                    Name = ButtonName,
                    DuplicatePolicy = NodeAttachmentDuplicatePolicy.ReuseExistingByName,
                    AttachParentSelector = screen => screen.GetNodeOrNull<Control>("InfoPanel")
                        ?? throw new InvalidOperationException("Character select info panel is unavailable.")
                });
        patcher.RegisterPatch<JanusWorkshopVoteSelectionPatch>();
        patcher.RegisterPatch<JanusWorkshopVoteScreenPatch>();
        patcher.RegisterPatch<JanusWorkshopVoteInteractablePatch>();
    }

    public static JanusWorkshopVoteButton? GetButton(NCharacterSelectScreen screen) =>
        screen.GetNodeOrNull<JanusWorkshopVoteButton>($"InfoPanel/{ButtonName}");
}

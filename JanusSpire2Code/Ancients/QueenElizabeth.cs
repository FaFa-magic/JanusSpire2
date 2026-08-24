using Godot;
using JanusSpire2.JanusSpire2Code.Configs;
using JanusSpire2.JanusSpire2Code.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace JanusSpire2.JanusSpire2Code.Ancients;

[RegisterSharedAncient]
public sealed class QueenElizabeth : ModAncientEventTemplate
{
    public override Color ButtonColor => new(0.12f, 0.2f, 0.8f, 0.5f);

    public override Color DialogueColor => new(0.12f, 0.2f, 0.8f);

    public override EventAssetProfile AssetProfile => new(
        BackgroundScenePath: "res://Test/scenes/test_ancient.tscn"
    );

    public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile => new(
        MapIconPath: "res://icon.svg",
        MapIconOutlinePath: "res://icon.svg",
        RunHistoryIconPath: "res://icon.svg",
        RunHistoryIconOutlinePath: "res://icon.svg"
    );

    private IReadOnlyList<EventOption> Pool1 =>
    [
        CreateModRelicOption<MagicMirror>(),
        CreateModRelicOption<MagicGloves>(),
        CreateModRelicOption<CatInBox>()
    ];

    private IReadOnlyList<EventOption> Pool2 =>
    [
        CreateModRelicOption<SealedTreasureChest>(),
        CreateModRelicOption<CrystalBall>(),
        CreateModRelicOption<BookAndPen>(),
        CreateModRelicOption<MagicWand>()
    ];

    private IReadOnlyList<EventOption> Pool3 =>
    [
        CreateModRelicOption<AfternoonTeaSupply>(),
        CreateModRelicOption<OverturnedTeaSet>(),
        CreateModRelicOption<DrinkCoupon>()
    ];

    public override IEnumerable<EventOption> AllPossibleOptions => [.. Pool1, .. Pool2, .. Pool3];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Rng.NextItem(Pool1)!,
            Rng.NextItem(Pool2)!,
            Rng.NextItem(Pool3)!
        ];
    }

    public override bool IsValidForAct(ActModel act)
    {
        return JanusConfigPage.QueenElizabethEnabledBinding.Read() &&
               act is Hive or Glory;
    }
}

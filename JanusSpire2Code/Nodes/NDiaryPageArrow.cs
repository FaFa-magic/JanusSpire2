using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace JanusSpire2.JanusSpire2Code.Nodes;

public partial class NDiaryPageArrow : NGoldArrowButton
{
    [Export]
    public bool PointRight { get; set; }

    public override void _Ready()
    {
        base._Ready();
        _icon.FlipH = PointRight;
    }
}

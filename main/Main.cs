using Godot;

public partial class Main : Node {
    public override void _Ready() {
        // setup signals
        GetNode<DebugMode>("/root/DebugMode").DebugOceanChanged += GetNode<Ocean>("Ocean").ConfigureTileDebugVisuals;

        GetNode<Player>("Player").PositionChangedSignificantly += GetNode<Ocean>("Ocean").OnOriginChanged;
        GameClock.ConnectGameSecondsChanged(GetNode<TimeDisplay>("/root/Main/HUD/TimeDisplay").Update);
    }
}

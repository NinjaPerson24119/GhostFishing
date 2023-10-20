using Godot;

internal partial class Main : Node {
    public override void _Ready() {
        Ocean ocean = DependencyInjector.Ref().GetOcean();
        TimeDisplay timeDisplay = DependencyInjector.Ref().GetTimeDisplay();
        PauseMenu pauseMenu = DependencyInjector.Ref().GetPauseMenu();

        // TODO: update ocean based on both players
        Player playerOne = PlayerInjector.Ref().GetPlayers()[PlayerID.One];
        playerOne.PositionChangedSignificantly += ocean.OnOriginChanged;

        GetNode<DebugMode>("/root/DebugMode").DebugOceanChanged += ocean.ConfigureTileDebugVisuals;
        GameClock.ConnectGameSecondsChanged(timeDisplay.Update);
    }
}

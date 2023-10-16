using Godot;

internal partial class Main : Node {
    public override void _Ready() {
        SetupSignals();
    }

    private void SetupSignals() {
        // TODO Hack: until we make these references support multiple players
        Player player = DependencyInjector.Ref().GetLocalPlayerContext("/Main/Pausable/PlayerContext-1/Player").Player;
        Ocean ocean = DependencyInjector.Ref().GetOcean();
        Controller controller = DependencyInjector.Ref().GetController();
        TimeDisplay timeDisplay = DependencyInjector.Ref().GetTimeDisplay();
        PlayerMenu playerMenu = DependencyInjector.Ref().GetPlayerMenu();
        PauseMenu pauseMenu = DependencyInjector.Ref().GetPauseMenu();
        FollowCamera followCamera = DependencyInjector.Ref().GetLocalPlayerContext("/Main/Pausable/PlayerContext-1/Player").FollowCamera;

        GetNode<DebugMode>("/root/DebugMode").DebugOceanChanged += ocean.ConfigureTileDebugVisuals;

        GameClock.ConnectGameSecondsChanged(timeDisplay.Update);

        player.PositionChangedSignificantly += ocean.OnOriginChanged;
        controller.SetPlayerControlsDisabled += player.SetControlsDisabled;
        controller.SetPlayerControlsDisabled += followCamera.SetControlsDisabled;
    }
}

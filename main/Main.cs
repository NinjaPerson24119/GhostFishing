using Godot;

internal partial class Main : Node {
    public override void _Ready() {
        Ocean ocean = DependencyInjector.Ref().GetOcean();
        CommonController commonController = DependencyInjector.Ref().GetCommonController();
        TimeDisplay timeDisplay = DependencyInjector.Ref().GetTimeDisplay();
        PauseMenu pauseMenu = DependencyInjector.Ref().GetPauseMenu();

        // TODO: update ocean based on both players
        Player playerOne = DependencyInjector.Ref().GetPlayerOne();
        PlayerContext playerOneContext = DependencyInjector.Ref().GetPlayerOneContext();
        playerOne.PositionChangedSignificantly += ocean.OnOriginChanged;

        GetNode<DebugMode>("/root/DebugMode").DebugOceanChanged += ocean.ConfigureTileDebugVisuals;
        GameClock.ConnectGameSecondsChanged(timeDisplay.Update);

        PlayerContext[] playerContexts = DependencyInjector.Ref().GetPlayerContexts();
        for (int i = 0; i < playerContexts.Length; i++) {
            commonController.SetPlayerControlsDisabled += playerContexts[i].Player.SetControlsDisabled;
            commonController.SetPlayerControlsDisabled += playerContexts[i].FollowCamera.SetControlsDisabled;
        }

    }
}

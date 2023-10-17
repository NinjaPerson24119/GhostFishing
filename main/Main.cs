using Godot;

internal partial class Main : Node {
    public override void _Ready() {
        // TODO Hack: until we make these references support multiple players
        Player player = DependencyInjector.Ref().GetPlayerOne();
        if (player.PlayerContext == null) {
            throw new System.Exception("PlayerContext (One) must be set before _Ready is called");
        }

        Ocean ocean = DependencyInjector.Ref().GetOcean();
        CommonController commonController = DependencyInjector.Ref().GetCommonController();
        TimeDisplay timeDisplay = DependencyInjector.Ref().GetTimeDisplay();
        PauseMenu pauseMenu = DependencyInjector.Ref().GetPauseMenu();

        // TODO hack
        PlayerMenu playerMenu = player.PlayerContext.PlayerMenu;
        FollowCamera followCamera = player.PlayerContext.FollowCamera;

        GetNode<DebugMode>("/root/DebugMode").DebugOceanChanged += ocean.ConfigureTileDebugVisuals;

        GameClock.ConnectGameSecondsChanged(timeDisplay.Update);

        player.PositionChangedSignificantly += ocean.OnOriginChanged;

        Player[] players = DependencyInjector.Ref().GetPlayers();
        for (int i = 0; i < players.Length; i++) {
            commonController.SetPlayerControlsDisabled += players[i].SetControlsDisabled;
            commonController.SetPlayerControlsDisabled += players[i].PlayerContext.FollowCamera.SetControlsDisabled;
        }

    }
}

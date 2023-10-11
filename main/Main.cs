using Godot;

public partial class Main : Node {
    public override void _Ready() {
        SetupSignals();
        AssignDefaults();
    }

    private void SetupSignals() {
        Player player = DependencyInjector.Ref().GetPlayer();
        Ocean ocean = DependencyInjector.Ref().GetOcean();
        Controller controller = DependencyInjector.Ref().GetController();
        TimeDisplay timeDisplay = DependencyInjector.Ref().GetTimeDisplay();
        PlayerMenu playerMenu = DependencyInjector.Ref().GetPlayerMenu();
        PauseMenu pauseMenu = DependencyInjector.Ref().GetPauseMenu();

        GetNode<DebugMode>("/root/DebugMode").DebugOceanChanged += ocean.ConfigureTileDebugVisuals;

        GameClock.ConnectGameSecondsChanged(timeDisplay.Update);

        player.PositionChangedSignificantly += ocean.OnOriginChanged;
        controller.SetPlayerControlsDisabled += player.SetControlsDisabled;
    }

    private void AssignDefaults() {
        // assign default inventory to player
        // player.Inventory = AssetManager.Ref().DefaultInventory;
    }
}

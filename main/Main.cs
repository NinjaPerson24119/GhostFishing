using Godot;

public partial class Main : Node {
    public override void _Ready() {
        SetupSignals();
        AssignDefaults();
    }

    private void SetupSignals() {
        GetNode<DebugMode>("/root/DebugMode").DebugOceanChanged += GetNode<Ocean>("Ocean").ConfigureTileDebugVisuals;

        Player player = DependencyInjector.Ref().GetPlayer();
        Ocean ocean = DependencyInjector.Ref().GetOcean();
        Controller controller = DependencyInjector.Ref().GetController();

        player.PositionChangedSignificantly += ocean.OnOriginChanged;
        controller.ControlsContextChanged += player.OnControlsContextChanged;

        DependencyInjector.Ref().GetPlayerMenu().CloseMenu += controller.OnMenuClosed;
        DependencyInjector.Ref().GetPauseMenu().CloseMenu += controller.OnMenuClosed;
        controller.OpenInventory += DependencyInjector.Ref().GetPlayerMenu().OnOpenInventory;

        TimeDisplay timeDisplay = DependencyInjector.Ref().GetTimeDisplay();
        GameClock.ConnectGameSecondsChanged(timeDisplay.Update);
    }

    private void AssignDefaults() {
        // assign default inventory to player
        // player.Inventory = AssetManager.Ref().DefaultInventory;
    }
}

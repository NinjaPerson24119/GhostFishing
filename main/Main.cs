using Godot;

public partial class Main : Node {
    private Player _player;
    private Ocean _ocean;
    private Controller _controller;
    private PlayerMenu _playerMenu;
    private PauseMenu _pauseMenu;
    private TimeDisplay _timeDisplay;

    public override void _Ready() {
        InjectDependencies();
        SetupSignals();
        AssignDefaults();
    }

    private void InjectDependencies() {
        _player = DependencyInjector.Ref().GetPlayer();
        _ocean = DependencyInjector.Ref().GetOcean();
        _controller = DependencyInjector.Ref().GetController();
        _playerMenu = DependencyInjector.Ref().GetPlayerMenu();
        _pauseMenu = DependencyInjector.Ref().GetPauseMenu();
        _timeDisplay = DependencyInjector.Ref().GetTimeDisplay();
    }

    private void SetupSignals() {
        GetNode<DebugMode>("/root/DebugMode").DebugOceanChanged += _ocean.ConfigureTileDebugVisuals;
        GameClock.ConnectGameSecondsChanged(_timeDisplay.Update);

        _player.PositionChangedSignificantly += _ocean.OnOriginChanged;

        // Attach controller to player and UI
        _controller.ControlsContextChanged += _player.OnControlsContextChanged;
        _controller.ControlsContextChanged += _playerMenu.OnControlsContextChanged;
        _controller.ControlsContextChanged += _pauseMenu.OnControlsContextChanged;

        _controller.ToggleViewInventory += _playerMenu.OnToggle;
        _controller.ToggleViewPauseMenu += _pauseMenu.OnToggle;

        _playerMenu.CloseMenu += _controller.OnMenuClosed;
        _pauseMenu.CloseMenu += _controller.OnMenuClosed;

        _controller.ControlsContext = ControlsContextType.Player;
    }

    private void AssignDefaults() {
        // assign default inventory to player
        // player.Inventory = AssetManager.Ref().DefaultInventory;
    }
}

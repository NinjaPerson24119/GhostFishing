using Godot;

public partial class Main : Node {
    private Player _player;
    private Ocean _ocean;
    private Controller _controller;
    private TimeDisplay _timeDisplay;
    private PlayerMenu _playerMenu;
    private PauseMenu _pauseMenu;

    public override void _Ready() {
        InjectDependencies();
        SetupSignals();
        AssignDefaults();
    }

    private void InjectDependencies() {
        _player = DependencyInjector.Ref().GetPlayer();
        _ocean = DependencyInjector.Ref().GetOcean();
        _controller = DependencyInjector.Ref().GetController();
        _timeDisplay = DependencyInjector.Ref().GetTimeDisplay();
        _playerMenu = DependencyInjector.Ref().GetPlayerMenu();
        _pauseMenu = DependencyInjector.Ref().GetPauseMenu();
    }

    private void SetupSignals() {
        GetNode<DebugMode>("/root/DebugMode").DebugOceanChanged += _ocean.ConfigureTileDebugVisuals;

        GameClock.ConnectGameSecondsChanged(_timeDisplay.Update);

        _player.PositionChangedSignificantly += _ocean.OnOriginChanged;
        _controller.SetPlayerControlsDisabled += _player.SetControlsDisabled;
    }

    private void AssignDefaults() {
        // assign default inventory to player
        // player.Inventory = AssetManager.Ref().DefaultInventory;
    }
}

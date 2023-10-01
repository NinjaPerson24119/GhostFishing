using Godot;

public partial class Controller : Node {
    private enum ControlsContextType {
        // player can open menus
        Player = 0,
        // player is in a menu so cannot be controlled
        PlayerMenu = 1,
        // pause menu open implies both player and player menus cannot be controlled
        PauseMenu = 2,
    }

    static SingletonTracker<Controller> _singletonTracker = new SingletonTracker<Controller>();
    private static Controller _singleton { get => _singletonTracker.Ref(); }
    public override void _Ready() {
        _singletonTracker.Ready(this);
    }

    private ControlsContextType ControlsContext {
        get => _controlsContext;
        set {
            _controlsContext = value;
            EmitSignal(SignalName.SetPlayerControlsDisabled, value != ControlsContextType.Player);
        }
    }
    private ControlsContextType _controlsContext = ControlsContextType.Player;

    [Signal]
    public delegate void SetPlayerControlsDisabledEventHandler(bool disabled);

    public override void _Process(double delta) {
        ProcessPlayerMenu();
        ProcessPauseMenu();
    }

    public override void _Input(InputEvent inputEvent) {
        TryOpenPlayerMenu(inputEvent);
        TryOpenPauseMenu(inputEvent);
    }

    public void TryOpenPlayerMenu(InputEvent inputEvent) {
        if (ControlsContext != ControlsContextType.Player) {
            return;
        }
        GD.Print("Try Open");
        if (inputEvent.IsActionPressed("open_inventory")) {
            ControlsContext = ControlsContextType.PlayerMenu;
            DependencyInjector.Ref().GetPlayerMenu().Open();
            GD.Print("Open Inventory");
            return;
        }
    }

    public void TryOpenPauseMenu(InputEvent inputEvent) {
        if (ControlsContext != ControlsContextType.Player && ControlsContext != ControlsContextType.PlayerMenu) {
            return;
        }
        if (inputEvent.IsActionPressed("open_pause_menu")) {
            ControlsContext = ControlsContextType.PauseMenu;
            DependencyInjector.Ref().GetPlayerMenu().Disabled = true;
            DependencyInjector.Ref().GetPauseMenu().Open();
            GD.Print("Open Pause Menu");
            return;
        }
    }

    public void ProcessPlayerMenu() {
        if (ControlsContext != ControlsContextType.PlayerMenu) {
            return;
        }
        Menu menu = DependencyInjector.Ref().GetPlayerMenu();
        if (menu.IsOpen && menu.RequestedClose) {
            menu.Close();
            ControlsContext = ControlsContextType.Player;
        }
    }

    public void ProcessPauseMenu() {
        if (ControlsContext != ControlsContextType.PauseMenu && ControlsContext != ControlsContextType.PlayerMenu) {
            return;
        }
        Menu pauseMenu = DependencyInjector.Ref().GetPauseMenu();
        if (pauseMenu.IsOpen && pauseMenu.RequestedClose) {
            pauseMenu.Close();

            Menu playerMenu = DependencyInjector.Ref().GetPlayerMenu();
            playerMenu.Disabled = false;
            ControlsContext = playerMenu.IsOpen ? ControlsContextType.PlayerMenu : ControlsContextType.Player;
        }
    }
}

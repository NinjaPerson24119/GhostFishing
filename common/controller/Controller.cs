using Godot;

public enum ControlsContextType {
    Invalid = 0,
    Player = 1,
    Menu = 2,
}

public partial class Controller : Node {
    static SingletonTracker<Controller> _singletonTracker = new SingletonTracker<Controller>();
    private static Controller _singleton { get => _singletonTracker.Ref(); }

    public ControlsContextType ControlsContext {
        get {
            return _controlsContext;
        }
        set {
            _controlsContext = value;
            EmitSignal(SignalName.ControlsContextChanged, (int)_controlsContext);
        }
    }
    private ControlsContextType _controlsContext = ControlsContextType.Invalid;

    [Signal]
    public delegate void ControlsContextChangedEventHandler(ControlsContextType controlsContext);
    [Signal]
    public delegate void OpenPauseMenuEventHandler();
    [Signal]
    public delegate void OpenInventoryEventHandler();

    public override void _Ready() {
        _singletonTracker.Ready(this);
    }

    public override void _Input(InputEvent inputEvent) {
        ControlMenus(inputEvent);
    }

    public void ControlMenus(InputEvent inputEvent) {
        // menu controls are only valid when the player has focus
        if (ControlsContext != ControlsContextType.Player) {
            return;
        }

        Menu[] menus = new Menu[] {
            DependencyInjector.Ref().GetPauseMenu(),
            DependencyInjector.Ref().GetPlayerMenu(),
        };
        foreach (Menu menu in menus) {
            if (menu.IsOpen()) {
                if (menu.CloseActions.Contains(inputEvent.AsText())) {
                    menu.Close();
                    ControlsContext = ControlsContextType.Player;
                    continue;
                }
            }
            else {
                if (menu.OpenActions.Contains(inputEvent.AsText())) {
                    ControlsContext = ControlsContextType.Menu;
                    menu.Open();
                    continue;
                }
            }
        }
    }
}

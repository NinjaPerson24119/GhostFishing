using Godot;

public enum ControlsContextType {
    Invalid = 0,
    Menu = 1,
    Player = 2,
}

public partial class Controller : Node {
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
    public delegate void ToggleViewPauseMenuEventHandler();
    [Signal]
    public delegate void ToggleViewInventoryEventHandler();

    public override void _Input(InputEvent inputEvent) {
        if (inputEvent.IsActionPressed("pause")) {
            ControlsContext = ControlsContextType.Menu;
            EmitSignal(SignalName.ToggleViewPauseMenu);
        }
        if (inputEvent.IsActionPressed("open_inventory")) {
            ControlsContext = ControlsContextType.Menu;
            EmitSignal(SignalName.ToggleViewInventory);
        }
    }

    public void OnMenuClosed() {
        ControlsContext = ControlsContextType.Player;
    }
}

using Godot;

public enum ControlsContextType {
    CONTROLS_CONTEXT_TYPE_INVALID = 0,
    CONTROLS_CONTEXT_TYPE_MENU = 1,
    CONTROLS_CONTEXT_TYPE_PLAYER = 2,
}

public partial class Controller : Node {
    public ControlsContextType ControlsContext {
        get {
            return _controlsContext;
        }
        set {
            _controlsContext = value;
            EmitSignal(nameof(ControlsContextChangedEventHandler), (int)_controlsContext);
        }
    }
    private ControlsContextType _controlsContext = ControlsContextType.CONTROLS_CONTEXT_TYPE_PLAYER;

    [Signal]
    public delegate void ControlsContextChangedEventHandler(ControlsContextType controlsContext);
    [Signal]
    public delegate void OpenInventoryEventHandler();

    public override void _Input(InputEvent inputEvent) {
        if (inputEvent.IsActionPressed("open_inventory")) {
            ControlsContext = ControlsContextType.CONTROLS_CONTEXT_TYPE_MENU;
            EmitSignal(nameof(OpenInventoryEventHandler));
        }
    }

    public void OnMenuClosed() {
        ControlsContext = ControlsContextType.CONTROLS_CONTEXT_TYPE_PLAYER;
        // DEBUG
        GD.Print("Menu closed");
    }
}

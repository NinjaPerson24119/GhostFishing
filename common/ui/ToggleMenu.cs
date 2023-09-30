using Godot;

public partial class ToggleMenu : Control {
    private ControlsContextType _controlsContext = ControlsContextType.Invalid;

    [Signal]
    public delegate void CloseMenuEventHandler();

    public void OnControlsContextChanged(ControlsContextType controlsContext) {
        _controlsContext = controlsContext;
        if (_controlsContext != ControlsContextType.Menu) {
            Hide();
        }
    }

    public override void _Input(InputEvent inputEvent) {
        DebugTools.Assert(_controlsContext != ControlsContextType.Invalid, "ControlsContext is invalid. Did you forget to subscribe?");
        if (_controlsContext != ControlsContextType.Menu) {
            return;
        }
        if (inputEvent.IsActionPressed("cancel")) {
            EmitSignal(SignalName.CloseMenu);
        }
    }

    public void OnToggle() {
        Visible = !Visible;
    }
}

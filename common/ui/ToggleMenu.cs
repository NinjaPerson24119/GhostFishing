using Godot;

public partial class ToggleMenu : Control {
    private ControlsContextType _controlsContext = ControlsContextType.CONTROLS_CONTEXT_TYPE_INVALID;

    [Signal]
    public delegate void CloseMenuEventHandler();

    public void OnControlsContextChanged(ControlsContextType controlsContext) {
        GD.Print($"Context changed: {controlsContext}");
        _controlsContext = controlsContext;
        if (_controlsContext != ControlsContextType.CONTROLS_CONTEXT_TYPE_MENU) {
            Hide();
        }
    }

    public override void _Input(InputEvent inputEvent) {
        DebugTools.Assert(_controlsContext != ControlsContextType.CONTROLS_CONTEXT_TYPE_INVALID, "ControlsContext is invalid. Did you forget to subscribe?");

        GD.Print($"ctx: {_controlsContext} {inputEvent}");
        if (_controlsContext != ControlsContextType.CONTROLS_CONTEXT_TYPE_MENU) {
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

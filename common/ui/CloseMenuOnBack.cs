using Godot;

public partial class CloseMenuOnBack : Control {
    private ControlsContextType _controlsContext = ControlsContextType.CONTROLS_CONTEXT_TYPE_INVALID;

    [Signal]
    public delegate void CloseMenuEventHandler();

    public void OnControlsContextChanged(ControlsContextType controlsContext) {
        _controlsContext = controlsContext;
        if (_controlsContext != ControlsContextType.CONTROLS_CONTEXT_TYPE_MENU) {
            Hide();
        }
    }

    public override void _Input(InputEvent inputEvent) {
        if (_controlsContext != ControlsContextType.CONTROLS_CONTEXT_TYPE_MENU) {
            return;
        }
        if (inputEvent.IsActionPressed("cancel")) {
            EmitSignal(nameof(CloseMenuEventHandler));
        }
    }
}

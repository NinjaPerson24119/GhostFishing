using Godot;
using System.Collections.Generic;

// TODO: this is incompatible with pseudo-focus
public partial class Menu : Control {
    protected List<string> _closeActions = new List<string>();

    public bool IsOpen {
        get {
            return Visible;
        }
    }
    public bool AcceptingInput {
        get {
            return !(Disabled || RequestedClose || !IsOpen);
        }
    }
    public bool RequestedClose { get; private set; } = false;
    public bool Disabled = false;
    // use this in child classes to prevent the menu from closing when the cancel action is pressed
    protected bool CloseActionClosesMenu = true;

    [Signal]
    public delegate void OpenedEventHandler();

    public override void _Input(InputEvent inputEvent) {
        if (!AcceptingInput) {
            return;
        }
        if (CloseActionClosesMenu) {
            foreach (string action in _closeActions) {
                if (inputEvent.IsActionPressed(action)) {
                    RequestedClose = true;
                    break;
                }
            }
        }
    }

    public virtual void Open() {
        if (RequestedClose || IsOpen) {
            return;
        }
        Visible = true;
        EmitSignal(SignalName.Opened);
    }

    public virtual void Close() {
        Visible = false;
        RequestedClose = false;
    }

    public void RequestClose() {
        RequestedClose = true;
    }
}

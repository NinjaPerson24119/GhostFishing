using Godot;
using System.Collections.Generic;

public partial class Menu : Control {
    protected List<string> _closeActions = new List<string> { "cancel" };

    public bool IsOpen {
        get {
            return Visible;
        }
    }
    public bool RequestedClose { get; private set; } = false;
    public bool Disabled = false;

    public override void _Input(InputEvent inputEvent) {
        if (Disabled || RequestedClose || !IsOpen) {
            return;
        }
        foreach (string action in _closeActions) {
            if (inputEvent.IsActionPressed(action)) {
                RequestedClose = true;
                break;
            }
        }
    }

    public virtual void Open() {
        if (RequestedClose || IsOpen) {
            return;
        }
        Visible = true;
    }

    public virtual void Close() {
        Visible = false;
        RequestedClose = false;
    }
}

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

    private Timer _debounceTimer = new Timer() {
        WaitTime = 0.1f,
        OneShot = true,
    };

    public override void _Ready() {
        AddChild(_debounceTimer);
    }

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
        _ = TryOpen();
    }
    protected bool TryOpen() {
        if (RequestedClose || IsOpen || !_debounceTimer.IsStopped()) {
            return false;
        }
        _debounceTimer.Start();
        Visible = true;
        return true;
    }

    public virtual void Close() {
        _ = TryClose();
    }
    protected bool TryClose() {
        if (!_debounceTimer.IsStopped()) {
            return false;
        }
        _debounceTimer.Start();
        Visible = false;
        RequestedClose = false;
        return true;
    }
}

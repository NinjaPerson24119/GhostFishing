using Godot;
using System.Collections.Generic;

public partial class Menu : Control {
    // Set this to true if the Menu shouldn't mutate focus
    // This makes it compatible with PseudoFocusControl
    [Export]
    bool PseudoFocusCompatibility = false;

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
        SetDefaultFocus(InputTypeController.Ref().InputType);
        EmitSignal(SignalName.Opened);
    }

    public virtual void Close() {
        Visible = false;
        RequestedClose = false;
    }

    public void RequestClose() {
        RequestedClose = true;
    }

    public virtual Control? DefaultFocusElement() {
        return null;
    }

    public void SetDefaultFocus(InputType inputType) {
        if (PseudoFocusCompatibility) {
            return;
        }
        if (inputType == InputType.Joypad) {
            Control? defaultFocusElement = DefaultFocusElement();
            if (defaultFocusElement != null) {
                defaultFocusElement.GrabFocus();
            }
        }
        else {
            // bit of a hack, but removes focus from whoever had it before
            GrabFocus();
            ReleaseFocus();
        }
    }

    public void OnInputTypeChanged(InputType inputType) {
        if (PseudoFocusCompatibility) {
            return;
        }
        SetDefaultFocus(inputType);
    }
}

using Godot;

public partial class PseudoFocusContext : Node {
    PseudoFocusControl? _currentPseudoFocus = null;
    public void GrabPseudoFocus(PseudoFocusControl control) {
        if (_currentPseudoFocus != null) {
            _currentPseudoFocus.EmitSignal(PseudoFocusControl.SignalName.PseudoFocusEntered);
        }
        _currentPseudoFocus = control;
    }

    public void ReleasePseudoFocus(PseudoFocusControl control) {
        if (_currentPseudoFocus == control) {
            _currentPseudoFocus.EmitSignal(PseudoFocusControl.SignalName.PseudoFocusExited);
            _currentPseudoFocus = null;
        }
        else {
            GD.PrintErr("Attempted to release pseudo-focus on a control that doesn't have it");
        }
    }

    public bool HasPseudoFocus(PseudoFocusControl control) {
        return _currentPseudoFocus == control;
    }
}

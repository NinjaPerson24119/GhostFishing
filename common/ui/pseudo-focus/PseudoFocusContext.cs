using Godot;

public partial class PseudoFocusContext : Node {
    PseudoFocusControl? _currentPseudoFocus = null;
    public void GrabPseudoFocus(PseudoFocusControl control) {
        _currentPseudoFocus = control;
        if (_currentPseudoFocus != null) {
            _currentPseudoFocus.EmitSignal(PseudoFocusControl.SignalName.PseudoFocusEntered);
        }
    }

    public void ReleasePseudoFocus(PseudoFocusControl control) {
        if (_currentPseudoFocus == control) {
            var oldPseudoFocus = _currentPseudoFocus;
            _currentPseudoFocus = null;
            oldPseudoFocus.EmitSignal(PseudoFocusControl.SignalName.PseudoFocusExited);
        }
        else {
            GD.PrintErr("Attempted to release pseudo-focus on a control that doesn't have it");
        }
    }

    public bool HasPseudoFocus(PseudoFocusControl control) {
        return _currentPseudoFocus == control;
    }
}

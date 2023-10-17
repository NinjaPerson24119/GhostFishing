using Godot;

public partial class PseudoFocusContext : Node {
    PseudoFocusControl? _currentPseudoFocus = null;
    public void GrabPseudoFocus(PseudoFocusControl control) {
        if (_currentPseudoFocus != null) {
            _currentPseudoFocus.ReleasePseudoFocus();
        }
        _currentPseudoFocus = control;
        _currentPseudoFocus.GrabPseudoFocus();
    }
    public void ReleasePseudoFocus(PseudoFocusControl control) {
        if (_currentPseudoFocus == control) {
            _currentPseudoFocus.ReleasePseudoFocus();
            _currentPseudoFocus = null;
        }
    }
}

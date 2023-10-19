using Godot;

public partial class PseudoFocusContext : Node {
    PseudoFocusControl? _currentPseudoFocus = null;
    public void GrabPseudoFocus(PseudoFocusControl control) {
        if (_currentPseudoFocus != null) {
            _currentPseudoFocus.TreeExited -= OnCurrentFocusExitingTree;
        }
        _currentPseudoFocus = control;
        if (_currentPseudoFocus != null) {
            _currentPseudoFocus.EmitSignal(PseudoFocusControl.SignalName.PseudoFocusEntered);
            _currentPseudoFocus.TreeExited += OnCurrentFocusExitingTree;
        }
    }

    public void ReleasePseudoFocus(PseudoFocusControl control) {
        if (_currentPseudoFocus == control) {
            var oldPseudoFocus = _currentPseudoFocus;
            _currentPseudoFocus = null;
            oldPseudoFocus.TreeExited -= OnCurrentFocusExitingTree;
            oldPseudoFocus.EmitSignal(PseudoFocusControl.SignalName.PseudoFocusExited);
        }
        else {
            GD.PrintErr("Attempted to release pseudo-focus on a control that doesn't have it");
        }
    }

    public bool HasPseudoFocus(PseudoFocusControl control) {
        return _currentPseudoFocus == control;
    }

    public void OnCurrentFocusExitingTree() {
        GD.Print("Called OnCurrentFocusExitedTree");
        if (_currentPseudoFocus != null) {
            _currentPseudoFocus = null;
            GD.Print("Current pseudo-focus exited tree. Cleared from context.");
        }
    }
}

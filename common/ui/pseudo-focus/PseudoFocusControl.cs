using Godot;

public partial class PseudoFocusControl : Control {
    // Godot doesn't support multiple focuses for split-screen, so we need to fake it
    // We use a PseudoFocusContext to track which control has focus
    private PseudoFocusContext? _pseudoFocusContext;

    [Signal]
    public delegate void PseudoFocusEnteredEventHandler();
    [Signal]
    public delegate void PseudoFocusExitedEventHandler();

    public void GrabPseudoFocus() {
        if (_pseudoFocusContext == null) {
            throw new System.Exception("PseudoFocusControl must be added to a PseudoFocusContext");
        }
        _pseudoFocusContext.GrabPseudoFocus(this);
    }
    public void ReleasePseudoFocus() {
        if (_pseudoFocusContext == null) {
            throw new System.Exception("PseudoFocusControl must be added to a PseudoFocusContext");
        }
        _pseudoFocusContext.ReleasePseudoFocus(this);
    }
    public bool HasPseudoFocus() {
        if (_pseudoFocusContext == null) {
            throw new System.Exception("PseudoFocusControl must be added to a PseudoFocusContext");
        }
        return _pseudoFocusContext.HasPseudoFocus(this);
    }

    public PseudoFocusControl() {
        FocusMode = FocusModeEnum.None;
    }

    public override void _Ready() {
        _pseudoFocusContext = DependencyInjector.Ref().GetLocalPseudoFocusContext(GetPath());
    }
}

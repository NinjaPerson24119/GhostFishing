using Godot;

public partial class PseudoFocusControl : Control {
    // Godot doesn't support multiple focuses for split-screen, so we need to fake it
    [Signal]
    public delegate void PseudoFocusEnteredEventHandler();
    [Signal]
    public delegate void PseudoFocusExitedEventHandler();
    protected void SetPseudoFocus(bool value) {
        if (value == _hasPseudoFocus) {
            return;
        }
        _hasPseudoFocus = value;
        if (_hasPseudoFocus) {
            EmitSignal(SignalName.PseudoFocusEntered);
        }
        else {
            EmitSignal(SignalName.PseudoFocusExited);
        }
    }
    public void GrabPseudoFocus() {
        SetPseudoFocus(true);
    }
    public void ReleasePseudoFocus() {
        SetPseudoFocus(false);
    }
    public bool HasPseudoFocus() {
        return _hasPseudoFocus;
    }
    private bool _hasPseudoFocus = false;

    private PseudoFocusContext? _pseudoFocusContext;

    public PseudoFocusControl() {
        FocusMode = FocusModeEnum.None;
    }

    public override void _Ready() {
        _pseudoFocusContext = DependencyInjector.Ref().GetLocalPseudoFocusContext(GetPath());
    }
}

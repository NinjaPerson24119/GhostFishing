using Godot;

internal partial class PauseMenu : Menu {
    public override void _Ready() {
        base._Ready();
        _closeActions.Add("open_pause_menu");
    }
}

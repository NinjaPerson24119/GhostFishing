using Godot;

public partial class PauseMenu : Menu {
    public override void _Ready() {
        base._Ready();
        OpenActions.Add("pause");
        CloseActions.Add("pause");
    }
}

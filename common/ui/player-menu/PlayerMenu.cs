using Godot;

public partial class PlayerMenu : Menu {
    public override void _Ready() {
        base._Ready();
        _closeActions.Add("open_inventory");
    }
}

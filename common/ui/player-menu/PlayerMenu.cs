using Godot;

public partial class PlayerMenu : Menu {
    public override void _Ready() {
        base._Ready();
        OpenActions.Add("open_inventory");
        CloseActions.Add("open_inventory");
    }
}

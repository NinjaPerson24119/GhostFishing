using Godot;

public partial class PlayerStatusUI : PseudoFocusControl {
    public override void _Ready() {
        Label label = GetNode<Label>("Label");
        int playerNumber = (int)DependencyInjector.Ref().GetLocalPlayerContext(GetPath()).PlayerID;
        label.Text = $"Player {playerNumber}";
    }
}

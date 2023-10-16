using Godot;

public partial class PlayerContext : Node {
    [Export]
    public CoopManager.PlayerID PlayerID { get; private set; } = CoopManager.PlayerID.Invalid;
    public PlayerStateView PlayerStateView {
        get => AssetManager.Ref().GetPlayerView(0);
    }
}

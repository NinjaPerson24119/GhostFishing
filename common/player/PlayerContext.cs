using Godot;

public partial class PlayerContext : Node {
    [Export]
    public CoopManager.PlayerID PlayerID { get; private set; } = CoopManager.PlayerID.Invalid;
    public PlayerStateView PlayerStateView {
        get => AssetManager.Ref().GetPlayerView(0);
    }
    public Player Player {
        get => GetNode<Player>("Player");
    }
    public FollowCamera FollowCamera {
        get => GetNode<FollowCamera>("FollowCamera");
    }

    public string SelectAction {
        get => $"select_{(int)PlayerID}";
    }
}

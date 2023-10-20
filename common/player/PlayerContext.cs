using Godot;

public partial class PlayerContext : Node {
    [Export]
    public PlayerID PlayerID { get; private set; } = PlayerID.Invalid;
    [Export]
    public Vector3 InitialGlobalPosition { get; private set; } = Vector3.Zero;

    public PlayerStateView PlayerStateView {
        get => AssetManager.Ref().GetPlayerView(PlayerID);
    }
    public Player Player {
        get => GetNode<Player>("Player");
    }

    public FollowCamera FollowCamera {
        get {
            return GetNode<FollowCamera>("FollowCamera");
        }
    }

    public PlayerMenu PlayerMenu {
        get => GetNode<PlayerMenu>("PseudoFocusContext/PlayerHUD/PlayerMenu");
    }
    public PlayerController Controller {
        get => GetNode<PlayerController>("PlayerController");
    }
}

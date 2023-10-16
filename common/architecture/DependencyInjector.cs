using Godot;

internal partial class DependencyInjector : Node {
    static SingletonTracker<DependencyInjector> _singletonTracker = new SingletonTracker<DependencyInjector>();
    private static DependencyInjector _singleton { get => _singletonTracker.Ref(); }

    public DependencyInjector() {
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready() {
        _singletonTracker.Ready(this);
    }

    public static DependencyInjector Ref() {
        return _singleton;
    }

    public Player GetPlayer() {
        return GetNode<Player>("/root/Main/Pausable/PlayerContext-1/Player");
    }

    public Ocean GetOcean() {
        return GetNode<Ocean>("/root/Main/Pausable/Ocean");
    }

    public TimeDisplay GetTimeDisplay() {
        return GetNode<TimeDisplay>("/root/Main/Pausable/GameUI/HUD/TimeDisplay");
    }

    public Controller GetController() {
        return GetNode<Controller>("/root/Main/Controller");
    }

    public PauseMenu GetPauseMenu() {
        return GetNode<PauseMenu>("/root/Main/PauseMenu");
    }

    public PlayerMenu GetPlayerMenu() {
        return GetNode<PlayerMenu>("/root/Main/Pausable/GameUI/PlayerMenu");
    }

    public FollowCamera GetFollowCamera() {
        return GetNode<FollowCamera>("/root/Main/Pausable/FollowCamera");
    }

    public TrackingServer GetTrackingServer() {
        return GetNode<TrackingServer>("/root/Main/Pausable/TrackingServer");
    }

    // do not provide other singletons. they provide themselves.

    // resources local to each player
    private T NearestResourceInSubtree<T>(string relativeToNodePath) where T : Node {
        Node? node = GetNodeOrNull(relativeToNodePath);
        if (node == null) {
            throw new System.Exception($"Could not find node {relativeToNodePath}");
        }
        if (node is T) {
            return (T)node;
        }
        Node? parent = node.GetParentOrNull<Node>();
        if (parent == null) {
            throw new System.Exception($"Could not find nearest resource of type {typeof(T).Name} in subtree of {relativeToNodePath}");
        }
        return NearestResourceInSubtree<T>(parent.GetPath());
    }

    public PlayerContext GetLocalPlayerContext(string relativeToNodePath) {
        return NearestResourceInSubtree<PlayerContext>(relativeToNodePath);
    }
}

using Godot;

public partial class CoopManager : Node {
    static SingletonTracker<CoopManager> _singletonTracker = new SingletonTracker<CoopManager>();
    private static CoopManager _singleton { get => _singletonTracker.Ref(); }

    public CoopManager() {
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready() {
        _singletonTracker.Ready(this);
    }
    public static CoopManager Ref() {
        return _singleton;
    }

    public int NoPlayers { get; private set; } = 1;

    // TODO: signals for players joining and leaving
}

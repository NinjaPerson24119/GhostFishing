using Godot;

public partial class DependencyInjector : Node {
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
        return GetNode<Player>("/root/Main/Pausable/Player");
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

    // do not provide other singletons. they provide themselves.
}

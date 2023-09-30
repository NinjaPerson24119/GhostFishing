using Godot;

public partial class DependencyInjector : Node {
    static SingletonTracker<DependencyInjector> _singletonTracker = new SingletonTracker<DependencyInjector>();
    private static DependencyInjector _singleton { get => _singletonTracker.Ref(); }
    public override void _Ready() {
        _singletonTracker.Ready(this);
    }
    public static DependencyInjector Ref() {
        return _singleton;
    }

    public Player GetPlayer() {
        return GetNode<Player>("/root/Main/Player");
    }

    public Ocean GetOcean() {
        return GetNode<Ocean>("/root/Main/Ocean");
    }

    public TimeDisplay GetTimeDisplay() {
        return GetNode<TimeDisplay>("/root/Main/UI/HUD/TimeDisplay");
    }

    public Controller GetController() {
        return GetNode<Controller>("/root/Main/Controller");
    }

    public PauseMenu GetPauseMenu() {
        return GetNode<PauseMenu>("/root/Main/UI/PauseMenu");
    }

    public PlayerMenu GetPlayerMenu() {
        return GetNode<PlayerMenu>("/root/Main/UI/PlayerMenu");
    }

    // do not provide other singletons. they provide themselves.
}

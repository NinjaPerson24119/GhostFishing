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

    const string PlayerNodePath = "/root/Main/Player";
    const string OceanNodePath = "/root/Main/Ocean";

    public static Player GetPlayer() {
        return _singleton.GetNode<Player>(PlayerNodePath);
    }

    public static Ocean GetOcean() {
        return _singleton.GetNode<Ocean>(OceanNodePath);
    }
}

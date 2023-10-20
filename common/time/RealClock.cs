using Godot;

internal partial class RealClock : Node {
    static SingletonTracker<RealClock> _singletonTracker = new SingletonTracker<RealClock>();
    public override void _Ready() {
        _singletonTracker.Ready(this);
    }
    private static RealClock _singleton { get => _singletonTracker.Ref(); }
    public static RealClock Ref() {
        return _singleton;
    }
    public bool Paused = false;

    // real time elapsed since the game started
    public double RealTime { get; private set; } = 0.0;
    public override void _Process(double delta) {
        if (!Paused) {
            RealTime += delta;
        }
    }
}

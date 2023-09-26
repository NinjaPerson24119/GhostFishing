using Godot;

public partial class RealClock : Node {
    static SingletonTracker<RealClock> _singletonTracker = new SingletonTracker<RealClock>();
    public override void _Ready() {
        _singletonTracker.Ready(this);
    }

    // real time elapsed since the game started
    public static double RealTime { get; private set; } = 0.0;
    public override void _Process(double delta) {
        RealTime += delta;
    }
}

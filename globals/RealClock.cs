using Godot;

public partial class RealClock : Node {
    // real time elapsed since the game started
    public static double RealTime { get; private set; } = 0.0;

    public override void _Process(double delta) {
        RealTime += delta;
    }
}

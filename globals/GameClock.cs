using Godot;

public partial class GameClock : Node {
    public static double Time { get; private set; } = 0.0;
    public override void _Process(double delta) {
        Time += delta;
    }
}

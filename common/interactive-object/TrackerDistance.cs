using Godot;

internal class TrackerDistance {
    public static float DistanceSquared(Vector3 a, Vector3 b) {
        a.Y = 0;
        b.Y = 0;
        return a.DistanceSquaredTo(b);
    }
}

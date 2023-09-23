using Godot;

public static class DebugTools {
    // TODO: this won't remove the condition evaluation from release builds
    public static void Assert(bool condition, string message = "") {
#if DEBUG
        if (!condition) {
            GD.PrintErr("Assertion failed: " + message);
        }
#endif
    }
}

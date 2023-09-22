using Godot;

public static class DebugTools
{
    public static void Assert(bool condition, string message = "")
    {
#if DEBUG
        if (!condition) {
            GD.PrintErr("Assertion failed: " + message);
            throw new System.Exception("Assertion failed: " + message);
        }
#endif
    }
}

using Godot;

// reference: https://github.com/godotengine/godot/issues/15993#issuecomment-567242789
public class FreezeUtil {
    public static void FreezeScene(Node node, bool freeze) {
        FreezeNode(node, freeze);
        foreach (Node c in node.GetChildren()) {
            FreezeScene(c, freeze);
        }
    }

    public static void FreezeNode(Node node, bool freeze) {
        // this doesn't pause IntegrateForces RIP
        node.SetProcess(!freeze);
        node.SetPhysicsProcess(!freeze);
        node.SetProcessInput(!freeze);
        node.SetProcessInternal(!freeze);
        node.SetProcessUnhandledInput(!freeze);
        node.SetProcessUnhandledKeyInput(!freeze);
        node.SetBlockSignals(!freeze);
        node.SetPhysicsProcessInternal(!freeze);
        node.SetProcessShortcutInput(!freeze);
    }
}

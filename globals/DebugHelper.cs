using Godot;

public partial class DebugHelper : Node {
    bool debugMode = false;

    public override void _Ready() {
        RenderingServer.SetDebugGenerateWireframes(true);
        GetNode<Label>("DebugIndicator").Visible = false;
    }

    public override void _Input(InputEvent inputEvent) {
        if (inputEvent.IsActionPressed("exit_to_desktop")) {
            GetTree().Quit();
        }
        if (inputEvent.IsActionPressed("debug_mode_toggle")) {
            debugMode = !debugMode;
            if (debugMode) {
                GetNode<Label>("DebugIndicator").Visible = true;
            }
            else {
                GetNode<Label>("DebugIndicator").Visible = false;
                GetViewport().DebugDraw = Viewport.DebugDrawEnum.Disabled;
            }
        }
        if (!debugMode) {
            return;
        }

        if (inputEvent.IsActionPressed("debug_wireframe")) {
            ToggleDebugDrawEnum(Viewport.DebugDrawEnum.Wireframe);
        }
        if (inputEvent.IsActionPressed("debug_normals")) {
            ToggleDebugDrawEnum(Viewport.DebugDrawEnum.NormalBuffer);
        }
    }

    private void ToggleDebugDrawEnum(Viewport.DebugDrawEnum debugDrawEnum) {
        Viewport v = GetViewport();
        if (v.DebugDraw == debugDrawEnum) {
            v.DebugDraw = Viewport.DebugDrawEnum.Disabled;
        }
        else {
            v.DebugDraw = debugDrawEnum;
        }
    }
}

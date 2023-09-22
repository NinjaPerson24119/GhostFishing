using Godot;
using System.Collections.Generic;

public partial class DebugMode : Node {
    bool debugMode = false;
    Dictionary<string, Viewport.DebugDrawEnum> debugDrawMappings = new Dictionary<string, Viewport.DebugDrawEnum>{
        {"debug_wireframe", Viewport.DebugDrawEnum.Wireframe},
        {"debug_normals", Viewport.DebugDrawEnum.NormalBuffer},
        {"debug_overdraw", Viewport.DebugDrawEnum.Overdraw}
    };

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

        foreach (KeyValuePair<string, Viewport.DebugDrawEnum> kv in debugDrawMappings) {
            if (inputEvent.IsActionPressed(kv.Key)) {
                ToggleDebugDrawEnum(kv.Value);
            }
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

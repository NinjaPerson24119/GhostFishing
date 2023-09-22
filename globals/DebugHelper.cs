using Godot;
using System;

public partial class DebugHelper : Node {
    public override void _Ready() {
        RenderingServer.SetDebugGenerateWireframes(true);
    }

    public override void _Input(InputEvent inputEvent) {
        if (inputEvent.IsActionPressed("debug_toggle_wireframe")) {
            Viewport v = GetViewport();
            if (v.DebugDraw == Viewport.DebugDrawEnum.Wireframe) {
                v.DebugDraw = Viewport.DebugDrawEnum.Disabled;
            }
            else {
                v.DebugDraw = Viewport.DebugDrawEnum.Wireframe;
            }
        }
    }
}

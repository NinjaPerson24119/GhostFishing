using Godot;
using System.Collections.Generic;

public partial class DebugMode : Node {
    private bool debugMode = false;

    [Signal]
    public delegate void DebugOceanChangedEventHandler(bool debugOcean);
    public bool DebugOcean {
        get {
            return _debugOcean;
        }
        private set {
            _debugOcean = value;
            EmitSignal(SignalName.DebugOceanChanged, value);
        }
    }
    private bool _debugOcean = false;

    Dictionary<string, Viewport.DebugDrawEnum> debugDrawMappings = new Dictionary<string, Viewport.DebugDrawEnum>{
        {"debug_wireframe", Viewport.DebugDrawEnum.Wireframe},
        {"debug_normals", Viewport.DebugDrawEnum.NormalBuffer},
        {"debug_overdraw", Viewport.DebugDrawEnum.Overdraw}
    };

    public override void _Ready() {
        RenderingServer.SetDebugGenerateWireframes(true);
        GetNode<Label>("DebugIndicator").Visible = false;
    }

    public override void _Process(double delta) {
        GetNode<Label>("DebugIndicator").Text = $"DEBUG MODE\n{Engine.GetFramesPerSecond()} FPS";
    }

    public override void _Input(InputEvent inputEvent) {
        if (inputEvent.IsActionPressed("exit_to_desktop")) {
            GetTree().Quit();
        }
        if (inputEvent.IsActionPressed("debug_mode_toggle")) {
            ToggleDebugMode();
        }
        if (debugMode) {
            HandleDebugModeInput(inputEvent);
        }
    }

    private void ToggleDebugMode() {
        debugMode = !debugMode;
        if (debugMode) {
            OnDebugModeEnabled();
        }
        else {
            OnDebugModeDisabled();
        }
    }

    private void OnDebugModeDisabled() {
        GetNode<Label>("DebugIndicator").Visible = false;
        GetViewport().DebugDraw = Viewport.DebugDrawEnum.Disabled;
        DebugOcean = false;
    }

    private void OnDebugModeEnabled() {
        GetNode<Label>("DebugIndicator").Visible = true;
    }

    private void HandleDebugModeInput(InputEvent inputEvent) {
        foreach (KeyValuePair<string, Viewport.DebugDrawEnum> kv in debugDrawMappings) {
            if (inputEvent.IsActionPressed(kv.Key)) {
                ToggleDebugDrawEnum(kv.Value);
            }
        }
        if (inputEvent.IsActionPressed("debug_ocean")) {
            DebugOcean = !DebugOcean;
            EmitSignal(SignalName.DebugOceanChanged, DebugOcean);
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

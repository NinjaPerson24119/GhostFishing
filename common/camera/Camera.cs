using Godot;

public partial class Camera : Camera3D {
    public override void _Ready() {
        PlayerInjector.Ref().SplitScreenChanged += OnSplitScreenChanged;
    }

    public override void _ExitTree() {
        PlayerInjector.Ref().SplitScreenChanged -= OnSplitScreenChanged;
    }

    private void UpdateFOV() {
        GD.Print("Update FOV");
        Fov = PlayerInjector.Ref().SplitScreenActive ? Constants.CameraFOVCoop : Constants.CameraFOVSinglePlayer;
    }

    public void OnSplitScreenChanged(bool splitScreenEnabled) {
        UpdateFOV();
    }
}

// JSON serializer relies on { get; set; } methods existing for each property.

internal class CommonSaveState {
    public double GameSeconds { get; set; }
}

internal partial class PlayerSaveState {
    public float GlobalPositionX { get; set; }
    public float GlobalPositionZ { get; set; }
    public float GlobalRotationY { get; set; }
}

internal partial class SaveState {
    public CommonSaveState? CommonSaveState { get; set; }
    public PlayerSaveState[]? PlayerSaveState { get; set; }
}

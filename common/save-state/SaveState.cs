// JSON serializer relies on { get; set; } methods existing for each property.

public class CommonState {
    public double GameSeconds { get; set; }
}

public partial class PlayerState {
    public float GlobalPositionX { get; set; }
    public float GlobalPositionZ { get; set; }
    public float GlobalRotationY { get; set; }
}

public partial class SaveState {
    public CommonState CommonState { get; set; }
    public PlayerState[] PlayerState { get; set; }
}

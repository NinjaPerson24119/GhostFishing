using System.Collections.Generic;

// JSON serializer relies on { get; set; } methods existing for each property.

public class CommonSaveState {
    public double GameSeconds { get; set; }
}

public partial class PlayerSaveState {
    public float GlobalPositionX { get; set; }
    public float GlobalPositionZ { get; set; }
    public float GlobalRotationY { get; set; }
}

public partial class SaveState {
    public CommonSaveState? CommonSaveState { get; set; }
    public PlayerSaveState[]? PlayerSaveState { get; set; }
    public Dictionary<string, InventoryDTO> InventoryStates { get; set; } = new Dictionary<string, InventoryDTO>();
}

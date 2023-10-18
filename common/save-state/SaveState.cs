using System.Collections.Generic;

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
    public int Version { get; set; }
    public CommonSaveState? CommonSaveState { get; set; }
    public Dictionary<PlayerID, PlayerSaveState> PlayerSaveState { get; set; } = new Dictionary<PlayerID, PlayerSaveState>();
    public Dictionary<string, InventoryInstanceDTO> InventoryStates { get; set; } = new Dictionary<string, InventoryInstanceDTO>();
}

using Godot;

// "glue" actions to integrate subsystems in the player context
public partial class PlayerContext : Node {
    public bool OpenInventoryWithOthers(InventoryInstance[] inventories) {
        return PlayerMenu.OpenInventoryWithOthers(inventories);
    }
}

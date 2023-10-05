using Godot;

public partial class InventoryItemControl : TextureRect {
    public string InventoryItemInstanceID { get; private set; }

    public InventoryItemControl(string itemInstanceID) {
        InventoryItemInstanceID = itemInstanceID;
    }

    public void OnInventoryTileGlobalPositionChanged(Vector2 globalPosition) {
        GlobalPosition = globalPosition;
    }
}

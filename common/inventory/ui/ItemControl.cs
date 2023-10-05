using Godot;

public partial class InventoryItemControl : TextureRect {
    public void OnInventoryTileGlobalPositionChanged(Vector2 globalPosition) {
        GD.Print("Updated potatoes");
        GlobalPosition = globalPosition;
    }
}

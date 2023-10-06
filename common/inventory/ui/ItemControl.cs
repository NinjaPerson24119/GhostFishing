using Godot;

public partial class InventoryItemControl : TextureRect {
    public string InventoryItemInstanceID { get; private set; }
    private InventoryTile _anchorTile;

    public InventoryItemControl(string itemInstanceID, InventoryTile anchorTile) {
        InventoryItemInstanceID = itemInstanceID;
        _anchorTile = anchorTile;

        _anchorTile.GlobalPositionChanged += OnInventoryTileGlobalPositionChanged;
    }

    public override void _ExitTree() {
        _anchorTile.GlobalPositionChanged -= OnInventoryTileGlobalPositionChanged;
    }

    public void OnInventoryTileGlobalPositionChanged(Vector2 globalPosition) {
        GlobalPosition = globalPosition;
    }
}

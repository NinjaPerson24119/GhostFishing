using Godot;
using System.Collections.Generic;

public partial class InventoryGrid : Node2D {
    private Inventory _inventory;
    private int _tileSizePx = 64;
    private Color _defaultTileColor;
    private Color _backgroundColor;
    private List<InventoryTile> _tiles = new List<InventoryTile>();

    public InventoryGrid(Inventory inventory, int tileSizePx, Color defaultTileColor, Color backgroundColor) {
        _inventory = inventory;
        _tileSizePx = tileSizePx;
        _defaultTileColor = defaultTileColor;
        _backgroundColor = backgroundColor;

        _inventory.Updated += OnInventoryUpdated;
    }

    public override void _Ready() {
        for (int y = 0; y < _inventory.Height; y++) {
            for (int x = 0; x < _inventory.Width; x++) {
                bool spaceUsable = _inventory.SpaceUsable(x, y);
                bool spaceFilled = _inventory.SpaceFilled(x, y);

                Color color = _defaultTileColor;
                // get the item's background color if it has one
                if (spaceFilled) {
                    InventoryItemInstance? item = _inventory.ItemAt(x, y);
                    if (item == null) {
                        throw new System.Exception("Inventory ItemAt returned null but space is filled");
                    }
                    InventoryItemDefinition itemDef = AssetManager.Ref().GetInventoryItemDefinition(item.ItemDefinitionID);
                    color = itemDef.BackgroundColorOverride ?? _defaultTileColor;
                }

                Vector2 position = new Vector2(x * _tileSizePx, y * _tileSizePx);
                InventoryTile tile = new InventoryTile(position, _tileSizePx, color, _backgroundColor, spaceFilled, spaceUsable) {
                    Name = $"InventoryTile_{x}_{y}"
                };
                AddChild(tile);
                _tiles.Add(tile);
            }
        }
    }

    private void OnInventoryUpdated(Inventory.UpdateType updateType, string itemInstanceID) {
        // update filled state of tiles
        for (int y = 0; y < _inventory.Height; y++) {
            for (int x = 0; x < _inventory.Width; x++) {
                int idx = y * _inventory.Width + x;
                InventoryTile control = _tiles[idx];
                if (control is InventoryTile tile) {
                    bool isFilled = _inventory.SpaceFilled(x, y);
                    tile.IsFilled = isFilled;
                }
            }
        }
    }
}

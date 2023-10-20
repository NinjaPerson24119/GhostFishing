using Godot;
using System.Collections.Generic;

internal partial class InventoryGrid : Node2D {
    internal struct TileAppearanceOverride {
        public Vector2I Position;
        public Color TileColor;
        public bool Filled;
        public bool Visible;
    }

    private InventoryInstance _inventory;
    private int _tileSizePx = 64;
    private Color _defaultTileColor;
    private Color _backgroundColor;
    private List<InventoryTile> _tiles = new List<InventoryTile>();
    private List<TileAppearanceOverride> _tileAppearanceOverrides = new List<TileAppearanceOverride>();

    public InventoryGrid(InventoryInstance inventory, int tileSizePx, Color defaultTileColor, Color backgroundColor) {
        _inventory = inventory;
        _tileSizePx = tileSizePx;
        _defaultTileColor = defaultTileColor;
        _backgroundColor = backgroundColor;

        _inventory.Updated += OnInventoryUpdated;
    }

    public override void _ExitTree() {
        _inventory.Updated -= OnInventoryUpdated;
    }

    public override void _Ready() {
        for (int y = 0; y < _inventory.Height; y++) {
            for (int x = 0; x < _inventory.Width; x++) {
                bool spaceUsable = _inventory.SpaceUsable(x, y);
                bool spaceFilled = _inventory.SpaceFilled(x, y);
                Color color = GetTileFillColor(x, y);
                Vector2 position = new Vector2(x * _tileSizePx, y * _tileSizePx);
                InventoryTile tile = new InventoryTile(position, _tileSizePx, color, _backgroundColor, spaceFilled, spaceUsable) {
                    Name = $"InventoryTile_{x}_{y}"
                };
                AddChild(tile);
                _tiles.Add(tile);
            }
        }
    }

    private Color GetTileFillColor(int x, int y) {
        Color color = _defaultTileColor;
        bool spaceFilled = _inventory.SpaceFilled(x, y);
        if (spaceFilled) {
            InventoryItemInstance? item = _inventory.ItemAt(x, y);
            if (item == null) {
                throw new System.Exception("Inventory ItemAt returned null but space is filled");
            }
            if (item.BackgroundColor != null) {
                color = item.BackgroundColor.Value;
            }
        }
        return color;
    }

    private void OnInventoryUpdated(InventoryInstance.UpdateType updateType, string itemInstanceID) {
        // fill states might have changed due to items moving
        UpdateTileAppearances();
    }

    public void UpdateTileAppearances() {
        for (int y = 0; y < _inventory.Height; y++) {
            for (int x = 0; x < _inventory.Width; x++) {
                int idx = y * _inventory.Width + x;
                InventoryTile tile = _tiles[idx];
                tile.IsFilled = _inventory.SpaceFilled(x, y);
                tile.TileColor = GetTileFillColor(x, y);
                tile.Visible = _inventory.SpaceUsable(x, y);
            }
        }

        foreach (TileAppearanceOverride overrideData in _tileAppearanceOverrides) {
            int idx = overrideData.Position.Y * _inventory.Width + overrideData.Position.X;
            InventoryTile tile = _tiles[idx];
            tile.IsFilled = overrideData.Filled;
            tile.TileColor = overrideData.TileColor;
            tile.Visible = overrideData.Visible;
        }
    }

    public void ClearTileAppearanceOverrides() {
        _tileAppearanceOverrides.Clear();
    }

    public void SetTileAppearanceOverride(TileAppearanceOverride a) {
        if (a.Position.X < 0 || a.Position.X >= _inventory.Width || a.Position.Y < 0 || a.Position.Y >= _inventory.Height) {
            throw new System.Exception($"TileAppearanceOverride position {a.Position} is out of bounds");
        }
        _tileAppearanceOverrides.Add(a);
    }

    public Vector2I? GetTilePositionFromGlobalPosition(Vector2 globalPosition) {
        Vector2 topLeft = GlobalPosition;
        Vector2 bottomRight = topLeft + _tileSizePx * new Vector2(_inventory.Width, _inventory.Height);
        if (globalPosition.X < topLeft.X || globalPosition.X >= bottomRight.X || globalPosition.Y < topLeft.Y || globalPosition.Y >= bottomRight.Y) {
            return null;
        }
        Vector2 localPosition = globalPosition - topLeft;
        int x = (int)(localPosition.X / _tileSizePx);
        int y = (int)(localPosition.Y / _tileSizePx);
        if (x < 0 || x >= _inventory.Width || y < 0 || y >= _inventory.Height) {
            throw new System.Exception($"Tile position {x}, {y} is out of bounds ({_inventory.Width},{_inventory.Height}), but this should be impossible by construction");
        }
        return new Vector2I(x, y);
    }

    public Vector2 GetGlobalPositionFromTilePosition(Vector2I tilePosition) {
        return GlobalPosition + _tileSizePx * new Vector2(tilePosition.X, tilePosition.Y);
    }
}

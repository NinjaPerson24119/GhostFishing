using Godot;
using System.Collections.Generic;

public partial class InventoryGrid : GridContainer {
    private Inventory _inventory;
    private int _tileSizePx = 64;
    private Color _defaultTileColor;
    private Color _backgroundColor;
    private List<InventoryTile> _tileControls = new List<InventoryTile>();
    public bool IsInitialized {
        get {
            return _isInitialized;
        }
        set {
            _isInitialized = value;
            if (value) {
                EmitSignal(SignalName.Initialized);
                GD.Print($"InventoryGrid: initialized ({value})");
            }
        }
    }
    private bool _isInitialized = false;

    [Signal]
    public delegate void InitializedEventHandler();
    [Signal]
    public delegate void SelectedPositionChangedEventHandler(Vector2I position);

    public InventoryGrid(Inventory inventory, int tileSizePx, Color defaultTileColor, Color backgroundColor) {
        _inventory = inventory;
        _tileSizePx = tileSizePx;
        _defaultTileColor = defaultTileColor;
        _backgroundColor = backgroundColor;

        _inventory.Updated += OnInventoryUpdated;
    }

    public override void _Ready() {
        Columns = _inventory.Width;
        CustomMinimumSize = new Vector2(_inventory.Width * _tileSizePx, _inventory.Height * _tileSizePx);
        AddThemeConstantOverride("h_separation", 0);
        AddThemeConstantOverride("v_separation", 0);

        // add tiles to grid
        for (int y = 0; y < _inventory.Height; y++) {
            for (int x = 0; x < _inventory.Width; x++) {
                bool spaceUsable = _inventory.SpaceUsable(x, y);
                bool spaceFilled = _inventory.SpaceFilled(x, y);

                Color color = _defaultTileColor;
                // get the item's background color if it has one
                if (spaceFilled) {
                    InventoryItemInstance? item = _inventory.ItemAt(x, y);
                    if (item == null) {
                        throw new System.Exception("InventoryUI: item is null but space is filled");
                    }
                    InventoryItemDefinition itemDef = AssetManager.Ref().GetInventoryItemDefinition(item.ItemDefinitionID);
                    color = itemDef.BackgroundColorOverride ?? _defaultTileColor;
                }

                Vector2I position = new Vector2I(x, y);
                InventoryTile tile = new InventoryTile(position, color, _backgroundColor, spaceFilled, spaceUsable);
                tile.Focused += OnTileFocused;
                tile.Size = new Vector2(_tileSizePx, _tileSizePx);
                tile.CustomMinimumSize = new Vector2(_tileSizePx, _tileSizePx);

                AddChild(tile);
                _tileControls.Add(tile);
            }
        }

        // assign tile neighbors
        // the default grid assignments don't work because we have empty controls
        for (int y = 0; y < _inventory.Height; y++) {
            for (int x = 0; x < _inventory.Width; x++) {
                int index = y * _inventory.Width + x;
                InventoryTile tile = _tileControls[index];
                // top
                int neighborIdx = (y - 1) * _inventory.Width + x;
                tile.FocusNeighborTop = GetTileNodePath(neighborIdx);

                // bottom
                neighborIdx = (y + 1) * _inventory.Width + x;
                tile.FocusNeighborBottom = GetTileNodePath(neighborIdx);

                // left
                neighborIdx = y * _inventory.Width + (x - 1);
                tile.FocusNeighborLeft = GetTileNodePath(neighborIdx);

                // right
                neighborIdx = y * _inventory.Width + (x + 1);
                tile.FocusNeighborRight = GetTileNodePath(neighborIdx);
            }
        }

        IsInitialized = true;
    }

    private NodePath? GetTileNodePath(int idx) {
        // expected to handle index out of bounds when trying to get above the grid, for example
        if (idx < 0 || idx >= _tileControls.Count) {
            return null;
        }
        InventoryTile tile = _tileControls[idx];
        // pretend the tile doesn't exist if it's not visible
        if (!tile.Visible) {
            return null;
        }
        return tile.GetPath();
    }

    public InventoryTile GetTile(Vector2I position) {
        int idx = position.Y * _inventory.Width + position.X;
        if (idx < 0 || idx >= _tileControls.Count) {
            throw new System.Exception($"InventoryGrid: invalid tile position {position}. There are {_tileControls.Count} tiles.");
        }
        return _tileControls[idx];
    }

    private void OnTileFocused(Vector2I position) {
        // reducer
        EmitSignal(SignalName.SelectedPositionChanged, position);
        GD.Print($"Focused tile at {position}");
    }

    public void FocusFirstTile() {
        int i = 0;
        while (i < _tileControls.Count) {
            InventoryTile tile = _tileControls[i];
            if (tile.Visible) {
                tile.GrabFocus();
                return;
            }
            i++;
        }
        if (i == _tileControls.Count) {
            throw new System.Exception("Failed to focus first tile because no tiles were found.");
        }
    }

    private void OnInventoryUpdated(Inventory.UpdateType updateType, string itemInstanceID) {
        GD.Print("InventoryGrid: inventory updated");
        // update filled state of tiles
        for (int y = 0; y < _inventory.Height; y++) {
            for (int x = 0; x < _inventory.Width; x++) {
                int idx = y * _inventory.Width + x;
                InventoryTile tile = _tileControls[idx];
                bool isFilled = _inventory.SpaceFilled(x, y);
                tile.IsFilled = isFilled;
            }
        }
    }
}

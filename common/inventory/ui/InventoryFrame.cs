using Godot;
using System;
using System.Collections.Generic;

public partial class InventoryFrame : Control {
    public float ContainerWidthPx = 800;
    public float ContainerHeightPx = 800;
    // if this is set then the container dimensions are ignored
    public bool FitContainerToGrid = true;
    // this is the margin between the container and the grid (only applies if FitContainerToGrid is true)
    public int GridMarginPx = 30;
    public int TileSizePx { get; private set; }
    public Color BackgroundColor = new Color(0.0f, 0.0f, 0.0f);
    public Color DefaultTileColor = new Color(0.72f, 0.44f, 0.10f);
    public Color CanPlaceItemTileColor = new Color(0.0f, 1.0f, 0.0f);
    public Color CannotPlaceItemTileColor = new Color(1.0f, 0.0f, 0.0f);
    public Vector2I SelectedPosition {
        get {
            return _selectedPosition;
        }
        private set {
            Vector2I clampedPosition = value.Clamp(_selectionBoundTopLeft, _selectionBoundBottomRight);
            if (clampedPosition == _selectedPosition) {
                return;
            }
            _selectedPosition = clampedPosition;
            EmitSignal(SignalName.SelectedPositionChanged, _selectedPosition);
        }
    }
    private Vector2I _selectedPosition = Vector2I.Zero;
    public Vector2I selectionBoundTopLeft {
        get {
            return _selectionBoundTopLeft;
        }
        set {
            _selectionBoundTopLeft = value;
            // reassign selected position to clamp it
            SelectedPosition = SelectedPosition;
        }
    }
    private Vector2I _selectionBoundTopLeft = new Vector2I(0, 0);
    public Vector2I selectionBoundBottomRight {
        get {
            return _selectionBoundBottomRight;
        }
        set {
            _selectionBoundBottomRight = value;
            // reassign selected position to clamp it
            SelectedPosition = SelectedPosition;
        }
    }
    private Vector2I _selectionBoundBottomRight = new Vector2I(0, 0);

    private Inventory? _inventory;
    private InventoryGrid? _inventoryGrid;
    private List<InventoryItemSprite> _itemSprites = new List<InventoryItemSprite>();


    private Timer _inputRepeatDebounceTimer = new Timer() {
        WaitTime = 0.1f,
        OneShot = true,
    };
    private Timer _inputRepeatDelayTimer = new Timer() {
        WaitTime = 0.5f,
        OneShot = true,
    };
    private Dictionary<string, Vector2I> _inputActionToDirection = new Dictionary<string, Vector2I> {
            {"ui_up", new Vector2I(0, -1)},
            {"ui_down", new Vector2I(0, 1)},
            {"ui_left", new Vector2I(-1, 0)},
            {"ui_right", new Vector2I(1, 0)}
        };

    private string _containerFrameImagePath = "res://artwork/generated/ui/InventoryFrame.png";

    [Signal]
    public delegate void SelectedPositionChangedEventHandler(Vector2I position);

    public InventoryFrame(Inventory inventory, int tileSizePx) {
        FocusMode = FocusModeEnum.All;
        TileSizePx = tileSizePx;
        SetInventory(inventory, tileSizePx);
        AssetManager.Ref().PersistImage(_containerFrameImagePath);
    }

    public override void _Ready() {
        AddChild(_inputRepeatDebounceTimer);
        AddChild(_inputRepeatDelayTimer);
    }

    public override void _ExitTree() {
        if (_inventory != null) {
            _inventory.Updated -= OnInventoryUpdated;
        }
    }

    public override void _Input(InputEvent inputEvent) {
        if (_inventoryGrid == null) {
            return;
        }
        if (!HasFocus()) {
            return;
        }

        InputEventMouse? mouseEvent = inputEvent as InputEventMouse;
        if (mouseEvent != null) {
            Vector2I? selectedPosition = _inventoryGrid.GetTilePositionFromGlobalPosition(mouseEvent.GlobalPosition);
            if (selectedPosition != null) {
                SelectedPosition = selectedPosition.Value;
            }
        }
    }

    public override void _Process(double delta) {
        if (_inventory == null) {
            return;
        }
        if (!HasFocus()) {
            return;
        }

        foreach (KeyValuePair<string, Vector2I> entry in _inputActionToDirection) {
            if (Input.IsActionJustPressed(entry.Key)) {
                SelectedPosition = SelectedPosition + entry.Value;
                _inputRepeatDelayTimer.Start();
                _inputRepeatDebounceTimer.Start();
                break;
            }
            if (_inputRepeatDelayTimer.IsStopped() && _inputRepeatDebounceTimer.IsStopped() && Input.IsActionPressed(entry.Key)) {
                SelectedPosition = SelectedPosition + entry.Value;
                _inputRepeatDebounceTimer.Start();
                break;
            }
        }
    }

    public override void _Notification(int what) {
        switch (what) {
            case (int)NotificationMouseEnter:
                GrabFocus();
                break;
            case (int)NotificationMouseExit:
                ReleaseFocus();
                break;
        }
    }

    private void SetInventory(Inventory inventory, int tileSizePx) {
        if (_inventory != null) {
            _inventory.Updated -= OnInventoryUpdated;
        }
        _inventory = inventory;
        _inventory.Updated += OnInventoryUpdated;

        TileSizePx = tileSizePx;

        RespawnChildren();
        ResetSelectionBound();
        SelectDefaultPosition();
    }

    public void RespawnChildren() {
        FreeChildren();
        foreach (InventoryItemSprite itemSprite in _itemSprites) {
            itemSprite.QueueFree();
        }
        _itemSprites.Clear();

        SpawnFrame();
        SpawnItems();
    }

    private void FreeChildren() {
        foreach (Node child in GetChildren()) {
            RemoveChild(child);
            child.QueueFree();
        }
    }

    public void SpawnFrame() {
        if (_inventory == null) {
            throw new Exception("Cannot spawn children because inventory is null.");
        }
        GD.Print("Spawning inventory frame");
        DebugTools.Assert(TileSizePx > 0, "TileSizePx must be greater than 0");

        Vector2 gridSize = new Vector2(_inventory.Width * TileSizePx, _inventory.Height * TileSizePx);
        if (FitContainerToGrid) {
            ContainerWidthPx = gridSize.X + GridMarginPx * 2;
            ContainerHeightPx = gridSize.Y + GridMarginPx * 2;
        }

        if (_inventory.BackgroundImagePath != null) {
            TextureRect backgroundImage = new TextureRect() {
                Name = "BackgroundImage",
                Texture = GD.Load<Texture2D>(_inventory.BackgroundImagePath),
                Size = new Vector2(ContainerWidthPx, ContainerHeightPx),
            };
            CallDeferred("add_child", backgroundImage);
        }

        ColorRect containerBackgroundColor = new ColorRect() {
            Name = "ContainerBackgroundColor",
            Color = BackgroundColor,
            Size = new Vector2(ContainerWidthPx, ContainerHeightPx),
        };
        CallDeferred("add_child", containerBackgroundColor);

        // this is the outline of the frame
        TextureRect containerFrameImage = new TextureRect() {
            Name = "ContainerFrameImage",
            Texture = GD.Load<Texture2D>(_containerFrameImagePath),
            Size = new Vector2(ContainerWidthPx, ContainerHeightPx),
        };
        CallDeferred("add_child", containerFrameImage);

        _inventoryGrid = new InventoryGrid(
            inventory: _inventory,
            tileSizePx: TileSizePx,
            defaultTileColor: DefaultTileColor,
            backgroundColor: BackgroundColor
        ) {
            Name = "InventoryGrid",
            Position = new Vector2(GridMarginPx, GridMarginPx),
        };
        CallDeferred("add_child", _inventoryGrid);
    }

    public void SpawnItems() {
        if (_inventory == null) {
            throw new Exception("Cannot add items because inventory is null.");
        }
        if (_inventoryGrid == null) {
            throw new Exception("Cannot add items because inventory grid is null.");
        }

        GD.Print("Spawning inventory items");
        foreach (InventoryItemInstance item in _inventory.Items) {
            InventoryItemSprite itemSprite = BuildItemSprite(item);
            _inventoryGrid.CallDeferred("add_child", itemSprite);
        }
        GD.Print($"InventoryFrame: added {_itemSprites.Count} items");
    }

    public InventoryItemSprite BuildItemSprite(InventoryItemInstance item) {
        if (_inventoryGrid == null) {
            throw new Exception("Cannot build item sprite because inventory grid is null.");
        }

        InventoryItemDefinition itemDef = AssetManager.Ref().GetInventoryItemDefinition(item.ItemDefinitionID);

        float width = TileSizePx * itemDef.Space.Width;
        float height = TileSizePx * itemDef.Space.Height;
        Texture2D texture = GD.Load<Texture2D>(itemDef.ImagePath);
        Vector2 scale = new Vector2(width / texture.GetWidth(), height / texture.GetHeight());
        InventoryItemSprite sprite = new InventoryItemSprite(item.ItemInstanceID) {
            Name = $"InventoryItemSprite_{item.ItemInstanceID}",
            Texture = texture,
            Position = new Vector2(item.X, item.Y) * TileSizePx + new Vector2(width / 2, height / 2),
            Scale = scale,
            Centered = true,
            Rotation = item.RotationRadians,
        };
        _itemSprites.Add(sprite);

        return sprite;
    }

    private void OnInventoryUpdated(Inventory.UpdateType updateType, string itemInstanceID) {
        if (_inventory == null) {
            throw new Exception("Cannot update inventory because inventory is null.");
        }
        if (_inventoryGrid == null) {
            throw new Exception("Cannot update inventory because inventory grid is null.");
        }

        switch (updateType) {
            case Inventory.UpdateType.Place:
                InventoryItemInstance? item = _inventory.GetItemByID(itemInstanceID);
                if (item == null) {
                    throw new Exception("Inventory emitted place update with item instance ID that doesn't exist.");
                }
                InventoryItemSprite itemSprite = BuildItemSprite(item);
                _itemSprites.Add(itemSprite);
                _inventoryGrid.CallDeferred("add_child", itemSprite);
                break;
            case Inventory.UpdateType.Take:
                InventoryItemSprite? itemToRemove = null;
                foreach (InventoryItemSprite itemToCheck in _itemSprites) {
                    if (itemToCheck.ItemInstanceID == itemInstanceID) {
                        itemToRemove = itemToCheck;
                        break;
                    }
                }
                if (itemToRemove == null) {
                    throw new Exception("Inventory emitted take update with item instance ID that doesn't exist.");
                }
                int result = _itemSprites.RemoveAll(c => c.ItemInstanceID == itemInstanceID);
                if (result == 0) {
                    throw new Exception("Failed to remove item from list.");
                }
                GD.Print($"Item sprites remaining: {_itemSprites.Count}");
                GD.Print($"Freed {itemToRemove.ItemInstanceID}");
                itemToRemove.CallDeferred("free");
                break;
        }
        GD.Print("Inventory updated");
    }

    public void SelectDefaultPosition() {
        if (_inventory == null) {
            throw new Exception("Cannot select default position because inventory is null.");
        }
        for (int y = 0; y < _inventory.Height; y++) {
            for (int x = 0; x < _inventory.Width; x++) {
                if (_inventory.SpaceUsable(x, y)) {
                    SelectedPosition = new Vector2I(x, y);
                    return;
                }
            }
        }
    }

    public Vector2I SelectNearestTile(Vector2 globalPosition) {
        if (_inventoryGrid == null) {
            throw new Exception("Cannot get nearest global position because inventory grid is null.");
        }
        if (_inventory == null) {
            throw new Exception("Cannot get nearest global position because inventory is null.");
        }

        // this really does need to be O(n^2) because the inventory shape isn't necessarily a rectangle
        Vector2I nearestTile = Vector2I.Zero;
        float nearestDistance = float.MaxValue;
        for (int y = 0; y < _inventory.Height; y++) {
            for (int x = 0; x < _inventory.Width; x++) {
                // shift to tile center
                Vector2 tileGlobalPosition = _inventoryGrid.GlobalPosition + new Vector2(x, y) * TileSizePx + Vector2.One * TileSizePx / 2;
                if (globalPosition.DistanceTo(tileGlobalPosition) < nearestDistance) {
                    nearestTile = new Vector2I(x, y);
                    nearestDistance = globalPosition.DistanceTo(tileGlobalPosition);
                }
            }
        }
        SelectedPosition = nearestTile;
        return SelectedPosition;
    }

    public Vector2 GetSelectorGlobalPosition() {
        if (_inventoryGrid == null) {
            throw new Exception("Cannot get grid global position because inventory grid is null.");
        }
        return _inventoryGrid.GlobalPosition + new Vector2(SelectedPosition.X, SelectedPosition.Y) * TileSizePx;
    }

    public void ClearItemTilesAppearance() {
        if (_inventoryGrid == null) {
            throw new Exception("Cannot clear item tiles appearance because inventory grid is null.");
        }

        _inventoryGrid.ClearTileAppearanceOverrides();
        _inventoryGrid.UpdateTileAppearances();
    }

    public void SetItemTilesAppearance(InventoryItemInstance item) {
        if (_inventory == null) {
            throw new Exception("Cannot set item tiles appearance because inventory is null.");
        }
        if (_inventoryGrid == null) {
            throw new Exception("Cannot set item tiles appearance because inventory grid is null.");
        }

        InventoryItemDefinition itemDef = AssetManager.Ref().GetInventoryItemDefinition(item.ItemDefinitionID);
        for (int y = 0; y < itemDef.Space.Height; y++) {
            for (int x = 0; x < itemDef.Space.Width; x++) {
                if (!itemDef.Space.GetFilledMask(item.Rotation)[y * itemDef.Space.Width + x]) {
                    continue;
                }

                Vector2I tilePosition = new Vector2I(item.X + x, item.Y + y);
                Color color = CanPlaceItemTileColor;
                if (_inventory.SpaceFilled(tilePosition.X, tilePosition.Y) || !_inventory.SpaceUsable(tilePosition.X, tilePosition.Y)) {
                    color = CannotPlaceItemTileColor;
                }
                var appearanceOverride = new InventoryGrid.TileAppearanceOverride() {
                    Position = tilePosition,
                    Filled = true,
                    TileColor = color,
                    Visible = true,
                };
                _inventoryGrid.SetTileAppearanceOverride(appearanceOverride);
            };
        }
        _inventoryGrid.UpdateTileAppearances();
    }

    public void ResetSelectionBound() {
        if (_inventory == null) {
            throw new Exception("Cannot reset selection bound because inventory is null.");
        }
        _selectionBoundTopLeft = new Vector2I(0, 0);
        _selectionBoundBottomRight = new Vector2I(_inventory.Width - 1, _inventory.Height - 1);
    }

    // changing the bound may change the selected position so return it
    public Vector2I SetSelectionBound(Vector2I topLeft, Vector2I bottomRight) {
        if (_inventory == null) {
            throw new Exception("Cannot set selection bound because inventory is null.");
        }
        if (topLeft.X < 0 || topLeft.Y < 0 || bottomRight.X >= _inventory.Width || bottomRight.Y >= _inventory.Height) {
            throw new Exception("Cannot set selections bound beyond inventory bounds.");
        }
        selectionBoundTopLeft = topLeft;
        selectionBoundBottomRight = bottomRight;
        return SelectedPosition;
    }
}

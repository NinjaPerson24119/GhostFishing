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

    private Inventory? _inventory;
    private InventoryGrid? _inventoryGrid;
    private List<InventoryItemSprite> _itemSprites = new List<InventoryItemSprite>();
    public Vector2I SelectedPosition { get; private set; } = new Vector2I(0, 0);

    private string _containerFrameImagePath = "res://artwork/generated/ui/InventoryFrame.png";

    [Signal]
    public delegate void SelectedPositionChangedEventHandler(Vector2I position);

    public InventoryFrame(Inventory inventory, int tileSizePx) {
        FocusMode = FocusModeEnum.All;
        TileSizePx = tileSizePx;
        SetInventory(inventory, tileSizePx);
    }

    public override void _ExitTree() {
        if (_inventory != null) {
            _inventory.Updated -= OnInventoryUpdated;
        }
    }

    public override void _Input(InputEvent inputEvent) {
        if (_inventory == null) {
            return;
        }

        bool updated = false;
        if (inputEvent.IsActionPressed("ui_up")) {
            SelectedPosition = new Vector2I(SelectedPosition.X, SelectedPosition.Y - 1);
            updated = true;
        }
        else if (inputEvent.IsActionPressed("ui_down")) {
            SelectedPosition = new Vector2I(SelectedPosition.X, SelectedPosition.Y + 1);
            updated = true;
        }
        else if (inputEvent.IsActionPressed("ui_left")) {
            SelectedPosition = new Vector2I(SelectedPosition.X - 1, SelectedPosition.Y);
            updated = true;
        }
        else if (inputEvent.IsActionPressed("ui_right")) {
            SelectedPosition = new Vector2I(SelectedPosition.X + 1, SelectedPosition.Y);
            updated = true;
        }
        if (updated) {
            SelectedPosition = SelectedPosition.Clamp(new Vector2I(0, 0), new Vector2I(_inventory.Width - 1, _inventory.Height - 1));
            EmitSignal(SignalName.SelectedPositionChanged, SelectedPosition);
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
                    EmitSignal(SignalName.SelectedPositionChanged, SelectedPosition);
                    return;
                }
            }
        }
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
}

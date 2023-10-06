using Godot;
using System;
using System.Collections.Generic;

public partial class InventoryFrame : Control {
    [Export]
    public int ContainerWidthPx = 800;
    [Export]
    public int ContainerHeightPx = 800;
    // if this is set then the container dimensions are ignored
    [Export]
    public bool FitContainerToGrid = true;
    [Export]
    public int GridMarginPx = 40;
    [Export]
    public Color DefaultTileColor = new Color(0.72f, 0.44f, 0.10f, 0.5f);
    [Export]
    public int TileSizePx = 64;
    [Export]
    public Color BackgroundColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);

    private Inventory? _inventory;
    private InventoryGrid? _inventoryGrid;
    private List<InventoryItemControl> _itemControls = new List<InventoryItemControl>();

    private string _containerFrameImagePath = "res://artwork/generated/ui/InventoryFrame.png";

    [Signal]
    public delegate void SelectedPositionChangedEventHandler(Vector2I position);

    public InventoryFrame(Inventory inventory) {
        SetInventory(inventory);
    }

    public override void _Ready() {
        SpawnFrame();
    }

    public void SetInventory(Inventory inventory) {
        if (_inventory != null) {
            _inventory.Updated -= OnInventoryUpdated;
        }
        _inventory = inventory;
        _inventory.Updated += OnInventoryUpdated;
    }

    public void RespawnChildren() {
        FreeChildren();
        SpawnFrame();
    }

    private void FreeChildren() {
        foreach (Node child in GetChildren()) {
            RemoveChild(child);
            child.QueueFree();
        }
    }

    public void Focus() {
        if (_inventoryGrid == null) {
            throw new Exception("Cannot focus because inventory grid is null.");
        }
        _inventoryGrid.FocusFirstTile();
    }

    public void SpawnFrame() {
        if (GetChildCount() > 0) {
            throw new Exception("Cannot spawn children because there are already children.");
        }
        if (_inventory == null) {
            throw new Exception("Cannot spawn children because inventory is null.");
        }

        // create grid
        _inventoryGrid = new InventoryGrid(
            inventory: _inventory,
            tileSizePx: TileSizePx,
            defaultTileColor: DefaultTileColor,
            backgroundColor: BackgroundColor
        );
        _inventoryGrid.Initialized += SpawnGridDependents;

        // fit container dimensions
        if (FitContainerToGrid) {
            ContainerWidthPx = _inventory.Width * TileSizePx + GridMarginPx * 2;
            ContainerHeightPx = _inventory.Height * TileSizePx + GridMarginPx * 2;
        }

        // add a background image
        TextureRect? backgroundImage = null;
        if (_inventory.BackgroundImagePath != null) {
            backgroundImage = new TextureRect() {
                Texture = GD.Load<Texture2D>(_inventory.BackgroundImagePath),
                Size = new Vector2(ContainerWidthPx, ContainerHeightPx),
            };
        }

        // add a container color
        ColorRect containerBackgroundColor = new ColorRect() {
            Color = BackgroundColor,
            Size = new Vector2(ContainerWidthPx, ContainerHeightPx),
        };

        // add a container frame image
        TextureRect containerFrameImage = new TextureRect() {
            Texture = GD.Load<Texture2D>(_containerFrameImagePath),
            Size = new Vector2(ContainerWidthPx, ContainerHeightPx),
        };

        // center grid in container
        CenterContainer center = new CenterContainer() {
            Size = new Vector2(ContainerWidthPx, ContainerHeightPx),
        };

        // relay tile selection events
        _inventoryGrid.SelectedPositionChanged += OnSelectedPositionChanged;

        center.AddChild(_inventoryGrid);
        containerFrameImage.AddChild(center);
        if (backgroundImage != null) {
            containerBackgroundColor.AddChild(backgroundImage);
        }
        containerBackgroundColor.AddChild(containerFrameImage);
        CallDeferred("add_child", containerBackgroundColor);
    }

    public void SpawnGridDependents() {
        SpawnItems();
    }

    public void SpawnItems() {
        if (_inventory == null) {
            throw new Exception("Cannot add items because inventory is null.");
        }


        // add items to grid
        foreach (InventoryItemInstance item in _inventory.Items) {
            InventoryItemControl itemControl = BuildItemControl(item);
            CallDeferred("add_child", itemControl);
        }
        GD.Print($"InventoryFrame: added {_itemControls.Count} items");
    }

    public InventoryItemControl BuildItemControl(InventoryItemInstance item) {
        if (_inventoryGrid == null) {
            throw new Exception("Cannot build item control because inventory grid is null.");
        }
        if (_inventoryGrid.IsInitialized == false) {
            throw new Exception("Cannot build item control because inventory grid is not initialized.");
        }

        InventoryItemDefinition itemDef = AssetManager.Ref().GetInventoryItemDefinition(item.ItemDefinitionID);

        float width = TileSizePx * itemDef.Space.Width;
        float height = TileSizePx * itemDef.Space.Height;
        InventoryItemControl itemControl = new InventoryItemControl(item.ItemInstanceID) {
            Texture = GD.Load<Texture2D>(itemDef.ImagePath),
            Size = new Vector2(width, height),
            Rotation = item.RotationRadians,
        };
        itemControl.PivotOffset = itemControl.Size / 2;
        _itemControls.Add(itemControl);
        InventoryTile tile = _inventoryGrid.GetTile(new Vector2I(item.X, item.Y));
        tile.GlobalPositionChanged += itemControl.OnInventoryTileGlobalPositionChanged;

        return itemControl;
    }

    private void OnInventoryUpdated(Inventory.UpdateType updateType, string itemInstanceID) {
        if (_inventory == null) {
            throw new Exception("Cannot update inventory because inventory is null.");
        }

        switch (updateType) {
            case Inventory.UpdateType.Place:
                InventoryItemInstance? item = _inventory.GetItemByID(itemInstanceID);
                if (item == null) {
                    throw new Exception("Inventory emitted place update with item instance ID that doesn't exist.");
                }
                InventoryItemControl itemControl = BuildItemControl(item);
                _itemControls.Add(itemControl);
                CallDeferred("add_child", itemControl);
                break;
            case Inventory.UpdateType.Take:
                InventoryItemControl? itemControlToRemove = null;
                foreach (InventoryItemControl itemControlToCheck in _itemControls) {
                    if (itemControlToCheck.InventoryItemInstanceID == itemInstanceID) {
                        itemControlToRemove = itemControlToCheck;
                        break;
                    }
                }
                if (itemControlToRemove == null) {
                    throw new Exception("Inventory emitted take update with item instance ID that doesn't exist.");
                }
                int result = _itemControls.RemoveAll(c => c.InventoryItemInstanceID == itemInstanceID);
                if (result == 0) {
                    throw new Exception("Failed to remove item control from list.");
                }
                GD.Print($"Item controls remaining: {_itemControls.Count}");
                GD.Print($"Freed {itemControlToRemove.InventoryItemInstanceID}");
                itemControlToRemove.CallDeferred("free");
                break;
        }
        GD.Print("Inventory updated");
    }

    public void OnSelectedPositionChanged(Vector2I position) {
        // relay
        EmitSignal(SignalName.SelectedPositionChanged, position);
    }
}

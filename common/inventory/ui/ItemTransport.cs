using System;
using Godot;

public partial class InventoryItemTransport : Node2D {
    public class TakeItemAction {
        public Inventory From;
        public Inventory.Mutator FromMutator;
        public Vector2I TilePosition;
        public InventoryItemRotation Rotation;

        public TakeItemAction(Inventory from, Inventory.Mutator fromMutator, InventoryItemInstance item) {
            From = from;
            FromMutator = fromMutator;
            TilePosition = new Vector2I(item.X, item.Y);
            Rotation = item.Rotation;
        }
    }

    private TakeItemAction? _lastTake;
    // item we're currently holding
    private InventoryItemInstance? _item;
    // position of the tile we're currently hovering over in the inventory
    public Vector2I TilePosition;
    // inventory we're currently interacting with
    private Inventory? _inventory;
    private Inventory.Mutator? _mutator;
    // visual representation of inventory we need to send updates to
    private InventoryFrame? _frame;
    private InventoryItemTransportSelector _selector;
    private bool _inventoryFocused = false;
    private Timer _inventoryJustOpenedTimer = new Timer() {
        WaitTime = 0.1f
    };

    public InventoryItemTransport(int TileSize) {
        Name = "ItemTransport";
        _selector = new InventoryItemTransportSelector(TileSize) {
            Name = "Selector"
        };
        CallDeferred("add_child", _selector);
    }

    public override void _Ready() {
        DependencyInjector.Ref().GetController().InputTypeChanged += OnControllerInputTypeChanged;
        AddChild(_inventoryJustOpenedTimer);
    }

    public override void _ExitTree() {
        if (_mutator != null) {
            GD.PrintErr("InventoryItemTransport was not closed before being destroyed.");
            _mutator.Dispose();
        }
    }

    public override void _Input(InputEvent inputEvent) {
        if (_inventory == null || _frame == null) {
            return;
        }
        if (!_frame.HasFocus()) {
            InputEventMouse? mouseEvent = inputEvent as InputEventMouse;
            if (mouseEvent != null) {
                _selector.GlobalPosition = mouseEvent.GlobalPosition;
            }
        }
    }

    public void OpenInventory(Inventory inventory, InventoryFrame inventoryFrame) {
        if (_inventory != null || _mutator != null) {
            throw new Exception("Cannot open inventory because inventory and/or mutator is not null.");
        }

        _inventory = inventory;
        _mutator = inventory.GetMutator();
        if (_mutator == null) {
            throw new Exception("Failed to get mutator from inventory. It's probably already opened.");
        }
        _frame = inventoryFrame;
        _frame.SelectedPositionChanged += OnSelectedPositionChanged;

        _frame.FocusEntered += OnInventoryFocused;
        _frame.FocusExited += OnInventoryUnfocused;
        _frame.SelectedPositionChanged += OnSelectedPositionChanged;

        _inventoryJustOpenedTimer.Start();
        _frame.GrabFocus();
    }

    public void CloseInventory() {
        if (_frame == null) {
            throw new Exception("Cannot close inventory because frame is null.");
        }

        _frame.FocusEntered -= OnInventoryFocused;
        _frame.FocusExited -= OnInventoryUnfocused;
        _frame.SelectedPositionChanged -= OnSelectedPositionChanged;

        if (_inventory == null || _mutator == null) {
            throw new Exception("Cannot close inventory because inventory and/or mutator is null.");
        }
        // drop item if we have one
        RevertTakeItem();
        _mutator.Dispose();
        _mutator = null;
        _inventory = null;
    }

    public bool HasItem() {
        return _item != null;
    }

    public void PlaceItem() {
        PlaceItem(TilePosition);
    }
    private void PlaceItem(Vector2I tilePosition) {
        if (_item == null || _inventory == null || _mutator == null || !_inventoryFocused) {
            return;
        }
        _item.X = tilePosition.X;
        _item.Y = tilePosition.Y;
        bool result = _mutator.PlaceItem(_item);
        if (!result) {
            GD.Print("Can't place item");
            return;
        }
        ClearItem();
    }

    public void TakeItem() {
        if (_item != null || _inventory == null || _mutator == null || _frame == null || !_inventoryFocused) {
            return;
        }
        _item = _mutator.TakeItem(TilePosition.X, TilePosition.Y);
        if (_item == null) {
            GD.Print("Nothing to take");
            return;
        }
        _lastTake = new TakeItemAction(_inventory, _mutator, _item);

        InventoryItemDefinition itemDef = AssetManager.Ref().GetInventoryItemDefinition(_item.ItemDefinitionID);
        TilePosition = _frame.SetSelectionBound(new Vector2I(0, 0), new Vector2I(_inventory.Width - itemDef.Space.Width, _inventory.Height - itemDef.Space.Height));

        _selector.AssignItem(_item);
        SetItemTileAppearance();

        GD.Print("Took item (transport)");
    }

    public void RevertTakeItem() {
        if (_item == null || _lastTake == null) {
            return;
        }

        Inventory.Mutator? mutator = _lastTake.FromMutator;
        if (mutator == null) {
            throw new Exception("Failed to revert take because lastTake mutator is null.");
        }
        _item.X = _lastTake.TilePosition.X;
        _item.Y = _lastTake.TilePosition.Y;
        _item.Rotation = _lastTake.Rotation;
        bool result = mutator.PlaceItem(_item);
        if (!result) {
            throw new Exception("Failed to revert take. This should be impossible since we just took it.");
        }
        _lastTake = null;
        ClearItem();
    }

    public void RotateClockwise() {
        if (_item == null) {
            return;
        }
        _item.RotateClockwise();
        _selector.OnItemUpdated();
        if (_inventoryFocused) {
            SetItemTileAppearance();
        }
    }

    public void RotateCounterClockwise() {
        if (_item == null) {
            return;
        }
        _item.RotateCounterClockwise();
        _selector.OnItemUpdated();
        if (_inventoryFocused) {
            SetItemTileAppearance();
        }
    }

    private void ClearItem() {
        _item = null;
        _selector.UnassignItem();
        if (_frame != null) {
            _frame.ClearItemTilesAppearance();
            _frame.ResetSelectionBound();
        }
    }

    public void OnSelectedPositionChanged(Vector2I tilePosition) {
        if (_frame == null) {
            throw new Exception("Cannot change selected position because frame is null.");
        }

        TilePosition = tilePosition;

        _selector.GlobalPosition = _frame.GetSelectorGlobalPosition();
        SetItemTileAppearance();
    }

    public void SetItemTileAppearance() {
        if (_item == null || _frame == null) {
            return;
        }
        _item.X = TilePosition.X;
        _item.Y = TilePosition.Y;
        _frame.ClearItemTilesAppearance();
        _frame.SetItemTilesAppearance(_item);
    }

    public void OnInventoryFocused() {
        if (_frame == null) {
            throw new Exception("Cannot change inventory focus because frame is null.");
        }
        _inventoryFocused = true;

        ControllerInputType inputType = DependencyInjector.Ref().GetController().InputType;
        if (inputType == ControllerInputType.KeyboardMouse && _inventoryJustOpenedTimer.IsStopped()) {
            TilePosition = _frame.SelectNearestTile(_selector.GlobalPosition);
        }
        else if (inputType == ControllerInputType.Joypad) {
            TilePosition = _frame.SelectedPosition;
        }

        _selector.GlobalPosition = _frame.GetSelectorGlobalPosition();
        _selector.SetHoveringInventory(true);
        SetItemTileAppearance();
    }

    public void OnInventoryUnfocused() {
        if (_frame == null) {
            throw new Exception("Cannot change inventory focus because frame is null.");
        }
        _inventoryFocused = false;
        _selector.SetHoveringInventory(false);
        _frame.ClearItemTilesAppearance();
    }

    public void OnControllerInputTypeChanged(ControllerInputType inputType) {
        if (_frame == null) {
            return;
        }
        if (inputType == ControllerInputType.Joypad && !_inventoryFocused) {
            _frame.GrabFocus();
        }
    }
}

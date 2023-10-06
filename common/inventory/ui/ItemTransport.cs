using System;
using Godot;

public partial class InventoryItemTransport : Node2D {
    public class TakeItemAction {
        public Inventory From;
        public Inventory.Mutator FromMutator;
        public Vector2I TilePosition;

        public TakeItemAction(Inventory from, Inventory.Mutator fromMutator, Vector2I tilePosition) {
            From = from;
            FromMutator = fromMutator;
            TilePosition = tilePosition;
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

    public InventoryItemTransport(int TileSize) {
        Name = "ItemTransport";
        _selector = new InventoryItemTransportSelector(TileSize) {
            Name = "Selector"
        };
        CallDeferred("add_child", _selector);
    }

    public override void _ExitTree() {
        if (_mutator != null) {
            GD.PrintErr("InventoryItemTransport was not closed before being destroyed.");
            _mutator.Dispose();
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

        _selector.Visible = true;

        _frame.FocusEntered += OnInventoryFocused;
        _frame.FocusExited += OnInventoryUnfocused;
        _frame.SelectedPositionChanged += OnSelectedPositionChanged;
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
        if (_item != null) {
            RevertTakeItem();
        }
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
        if (_item == null || _inventory == null || _mutator == null) {
            return;
        }
        bool result = _mutator.PlaceItem(_item, tilePosition.X, tilePosition.Y);
        if (!result) {
            GD.Print("Can't place item");
            return;
        }
        _item = null;

        _selector.UnassignItem();
    }

    public void TakeItem() {
        if (_item != null || _inventory == null || _mutator == null) {
            return;
        }
        _item = _mutator.TakeItem(TilePosition.X, TilePosition.Y);
        if (_item == null) {
            GD.Print("Nothing to take");
            return;
        }
        _lastTake = new TakeItemAction(_inventory, _mutator, TilePosition);

        _selector.AssignItem(_item);
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
        bool result = mutator.PlaceItem(_item, _lastTake.TilePosition.X, _lastTake.TilePosition.Y);
        if (!result) {
            throw new Exception("Failed to revert take. This should be impossible since we just took it.");
        }
        _lastTake = null;
    }

    public void RotateClockwise() {
        if (_item == null) {
            throw new Exception("Cannot rotate because item is null.");
        }
        _item.RotateClockwise();
        _selector.OnItemUpdated();
    }

    public void RotateCounterClockwise() {
        if (_item == null) {
            throw new Exception("Cannot rotate because item is null.");
        }
        _item.RotateCounterClockwise();
        _selector.OnItemUpdated();
    }

    public void OnSelectedPositionChanged(Vector2I tilePosition) {
        if (_frame == null) {
            throw new Exception("Cannot change selected position because frame is null.");
        }

        GD.Print($"Transport tilePosition changed: {tilePosition}");
        TilePosition = tilePosition;
        _selector.GlobalPosition = _frame.GetSelectorGlobalPosition();
    }

    public void OnInventoryFocused() {
        InventoryFocused(true);
    }
    public void OnInventoryUnfocused() {
        InventoryFocused(false);
    }
    private void InventoryFocused(bool isFocused) {
        if (_frame == null) {
            throw new Exception("Cannot change inventory focus because frame is null.");
        }
        GD.Print($"Selector pos: {_frame.GetSelectorGlobalPosition()}");
        _selector.GlobalPosition = _frame.GetSelectorGlobalPosition();
        _selector.Visible = isFocused;
    }
}

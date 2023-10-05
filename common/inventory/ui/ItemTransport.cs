using System;
using Godot;

public class InventoryItemTransport {
    public class TakeItemAction {
        public Inventory From;
        public Inventory.Mutator FromMutator;
        public Vector2I Position;

        public TakeItemAction(Inventory from, Inventory.Mutator fromMutator, Vector2I position) {
            From = from;
            FromMutator = fromMutator;
            Position = position;
        }
    }

    public Vector2I Position;
    private TakeItemAction? _lastTake;
    private InventoryItemInstance? _item;
    private Inventory? _inventory;
    private Inventory.Mutator? _mutator;
    private InventoryFrame? _frame;

    public void OnSelectedPositionChanged(Vector2I position) {
        Position = position;
    }

    public bool HasItem() {
        return _item != null;
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
    }

    public void CloseInventory() {
        if (_inventory == null || _mutator == null) {
            throw new Exception("Cannot close inventory because inventory and/or mutator is null.");
        }
        // drop item if we have one
        if (_item != null) {
            RevertTakeItem();
        }
        _mutator.Release();
        _inventory = null;
        _mutator = null;
    }

    public void PlaceItem() {
        PlaceItem(Position);
    }
    private void PlaceItem(Vector2I position) {
        if (_item == null) {
            throw new Exception("Cannot place because item is null.");
        }
        if (_inventory == null) {
            throw new Exception("Cannot place because inventory is null.");
        }
        if (_mutator == null) {
            throw new Exception("Cannot place because mutator is null.");
        }
        bool result = _mutator.PlaceItem(_item, position.X, position.Y);
        if (!result) {
            GD.Print("Can't place item");
        }
        _item = null;
    }

    public void TakeItem() {
        if (_item != null) {
            throw new Exception("Cannot place because item is not null (already have an item).");
        }
        if (_inventory == null) {
            throw new Exception("Cannot place because inventory is null.");
        }
        if (_mutator == null) {
            throw new Exception("Cannot take because mutator is null.");
        }
        _lastTake = new TakeItemAction(_inventory, _mutator, Position);
        _item = _mutator.TakeItem(Position.X, Position.Y);
        if (_item == null) {
            GD.Print("Nothing to take");
        }
    }

    public void RevertTakeItem() {
        if (_item == null) {
            throw new Exception("Cannot revert take because item is null.");
        }
        if (_lastTake == null) {
            throw new Exception("Have item, but last take is null.");
        }

        Inventory.Mutator? mutator = _lastTake.From.GetMutator();
        if (mutator == null) {
            throw new Exception("Cannot revert take because ");
        }
        bool result = mutator.PlaceItem(_item, _lastTake.Position.X, _lastTake.Position.Y);
        if (!result) {
            throw new Exception("Failed to revert take.");
        }
        _lastTake = null;
    }

    public void RotateClockwise() {
        if (_item == null) {
            throw new Exception("Cannot rotate because item is null.");
        }
        _item.RotateClockwise();
    }

    public void RotateCounterClockwise() {
        if (_item == null) {
            throw new Exception("Cannot rotate because item is null.");
        }
        _item.RotateCounterClockwise();
    }
}

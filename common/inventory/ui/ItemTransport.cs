using System;
using Godot;

public class InventoryItemTransport {
    public class TakeItemAction {
        public Inventory From;
        public Inventory.Mutator FromMutator;
        public int X;
        public int Y;

        public TakeItemAction(Inventory from, Inventory.Mutator fromMutator, int x, int y) {
            From = from;
            FromMutator = fromMutator;
            X = x;
            Y = y;
        }
    }

    public int X;
    public int Y;
    private TakeItemAction? _lastTake;
    private InventoryItemInstance? _item;
    private Inventory? _inventory;
    private Inventory.Mutator? _mutator;

    public void OpenInventory(Inventory inventory) {
        if (_inventory != null || _mutator != null) {
            throw new Exception("Cannot open inventory because inventory and/or mutator is not null.");
        }

        _inventory = inventory;
        _mutator = inventory.GetMutator();
        if (_mutator == null) {
            throw new Exception("Failed to get mutator from inventory. It's probably already opened.");
        }
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
        PlaceItem(X, Y);
    }
    private void PlaceItem(int X, int Y) {
        if (_item == null) {
            throw new Exception("Cannot place because item is null.");
        }
        if (_inventory == null) {
            throw new Exception("Cannot place because inventory is null.");
        }
        if (_mutator == null) {
            throw new Exception("Cannot place because mutator is null.");
        }
        _mutator.PlaceItem(_item, X, Y);
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
        _lastTake = new TakeItemAction(_inventory, _mutator, X, Y);
        _item = _mutator.TakeItem(X, Y);
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
        mutator.PlaceItem(_item, _lastTake.X, _lastTake.Y);
        _lastTake = null;
    }
}

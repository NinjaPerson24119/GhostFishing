using System;
using Godot;
using System.Collections.Generic;
using System.Linq;

internal partial class InventoryItemTransport : Node2D {
    internal class OpenedInventory {
        public InventoryInstance Inventory;
        // visual representation of inventory we need to send updates to
        public InventoryFrame Frame;
        // we need a mutator to prevent multiple players from opening the same inventory
        public InventoryInstance.Mutator Mutator;

        public OpenedInventory(InventoryInstance inventory, InventoryFrame frame, InventoryInstance.Mutator mutator) {
            Inventory = inventory;
            Frame = frame;
            Mutator = mutator;
        }
    }
    internal class TakeItemAction {
        public InventoryInstance From;
        public InventoryInstance.Mutator FromMutator;
        public Vector2I TilePosition;
        public InventoryItemRotation Rotation;

        public TakeItemAction(InventoryInstance from, InventoryInstance.Mutator fromMutator, InventoryItemInstance item) {
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

    private List<string> _openedInventoryOrder = new List<string>();
    private Dictionary<string, OpenedInventory> _openedInventories = new Dictionary<string, OpenedInventory>();
    private OpenedInventory? _currentInventory = null;

    private InventoryItemTransportSelector _selector;
    private PlayerContext? _playerContext;
    private int _tileSize;

    public InventoryItemTransport(int TileSize) {
        Name = "ItemTransport";
        _selector = new InventoryItemTransportSelector(TileSize) {
            Name = "Selector"
        };
        _tileSize = TileSize;
        CallDeferred("add_child", _selector);
    }

    public override void _Ready() {
        _playerContext = DependencyInjector.Ref().GetLocalPlayerContext(GetPath());
        if (_playerContext == null) {
            throw new Exception("PlayerContext null");
        }
        _playerContext.Controller.InputTypeChanged += OnControllerInputTypeChanged;
    }

    public override void _ExitTree() {
        CloseInventories();
        if (_playerContext == null) {
            throw new Exception("PlayerContext null");
        }
        _playerContext.Controller.InputTypeChanged -= OnControllerInputTypeChanged;
    }

    public override void _Input(InputEvent inputEvent) {
        if (_playerContext == null) {
            throw new Exception("PlayerContext null");
        }
        if (_currentInventory == null && _item != null) {
            InputEventMouse? mouseEvent = inputEvent as InputEventMouse;
            if (mouseEvent != null && _playerContext.Controller.MouseAllowed()) {
                // shift cursor to center of item for more natural UX
                _selector.GlobalPosition = mouseEvent.GlobalPosition - new Vector2I(_item.Width, _item.Height) * _tileSize / 2;
            }
        }
    }

    public void OpenInventory(InventoryInstance inventory, InventoryFrame inventoryFrame) {
        if (_openedInventories.ContainsKey(inventory.InventoryInstanceID)) {
            throw new Exception("Failed to open inventory because it's already open.");
        }

        InventoryInstance.Mutator? mutator = inventory.GetMutator();
        if (mutator == null) {
            throw new Exception("Failed to get mutator from inventory. It's probably already opened.");
        }
        OpenedInventory openedInventory = new OpenedInventory(inventory, inventoryFrame, mutator);

        inventoryFrame.InventoryFocused += OnInventoryFocused;
        inventoryFrame.InventoryUnfocused += OnInventoryUnfocused;
        inventoryFrame.SelectedPositionChanged += OnSelectedPositionChanged;

        _openedInventories.Add(inventory.InventoryInstanceID, openedInventory);
        _openedInventoryOrder.Add(inventory.InventoryInstanceID);
    }

    public bool IsOpen(string inventoryInstanceID) {
        return _openedInventories.ContainsKey(inventoryInstanceID);
    }

    public void CloseInventories() {
        var openedInventoriesList = _openedInventories.ToList();
        foreach (KeyValuePair<string, OpenedInventory> entry in openedInventoriesList) {
            CloseInventory(entry.Key);
        }
        if (_item != null) {
            throw new Exception("Item is not null when closing inventory.");
        }
        _currentInventory = null;
    }

    public void CloseInventory(string inventoryInstanceID) {
        OpenedInventory? o = _openedInventories[inventoryInstanceID];
        if (o == null) {
            throw new Exception("Failed to close inventory because it's not open.");
        }
        if (_currentInventory != null && _currentInventory.Inventory.InventoryInstanceID == inventoryInstanceID) {
            _currentInventory = null;
        }

        if (o.Frame != null) {
            o.Frame.InventoryFocused -= OnInventoryFocused;
            o.Frame.InventoryUnfocused -= OnInventoryUnfocused;
            o.Frame.SelectedPositionChanged -= OnSelectedPositionChanged;
        }
        if (_lastTake != null && _lastTake.From.InventoryInstanceID == inventoryInstanceID) {
            RevertTakeItem();
        }
        if (o.Mutator != null) {
            o.Mutator.Dispose();
        }
        _openedInventories.Remove(inventoryInstanceID);
        _openedInventoryOrder.Remove(inventoryInstanceID);
    }

    public bool HasItem() {
        return _item != null;
    }

    public void PlaceItem() {
        PlaceItem(TilePosition);
    }
    private void PlaceItem(Vector2I tilePosition) {
        if (_item == null || _currentInventory == null) {
            return;
        }
        _item.X = tilePosition.X;
        _item.Y = tilePosition.Y;
        bool result = _currentInventory.Mutator.PlaceItem(_item);
        if (!result) {
            GD.Print("Can't place item");
            return;
        }

        // move mouse to first used tile of item so we don't have to move mouse to re-select it
        if (_currentInventory.Frame == null) {
            throw new Exception("Frame null");
        }
        if (_playerContext == null) {
            throw new Exception("PlayerContext null");
        }
        Vector2I firstTileOffset = _item.FirstUsedTileOffset();
        if (_playerContext.Controller.InputType == InputType.KeyboardMouse) {

            _currentInventory.Frame.MoveMouseToTile(new Vector2I(_item.X, _item.Y) + firstTileOffset);
        }
        else {
            _currentInventory.Frame.SelectedPosition = new Vector2I(_item.X, _item.Y) + firstTileOffset;
        }

        ClearItem();
    }

    public void TakeItem() {
        if (_item != null || _currentInventory == null) {
            return;
        }
        _item = _currentInventory.Mutator.TakeItem(TilePosition.X, TilePosition.Y);
        if (_item == null) {
            GD.Print("Nothing to take");
            return;
        }
        _lastTake = new TakeItemAction(_currentInventory.Inventory, _currentInventory.Mutator, _item);

        // move mouse to top-left of item so the item doesn't automatically move when we take it
        if (_playerContext == null) {
            throw new Exception("PlayerContext null");
        }
        if (_playerContext.Controller.InputType == InputType.KeyboardMouse) {
            _currentInventory.Frame.MoveMouseToTile(new Vector2I(_item.X, _item.Y));
        }
        else {
            _currentInventory.Frame.SelectedPosition = new Vector2I(_item.X, _item.Y);
        }

        // bounds need to be set for each frame to avoid edge conditions when transporting items between inventories
        TilePosition = _currentInventory.Frame.SetSelectionBound(new Vector2I(0, 0), new Vector2I(_currentInventory.Inventory.Width - _item.Width, _currentInventory.Inventory.Height - _item.Height));
        foreach (OpenedInventory openedInventory in _openedInventories.Values) {
            _ = openedInventory.Frame.SetSelectionBound(new Vector2I(0, 0), new Vector2I(openedInventory.Inventory.Width - _item.Width, openedInventory.Inventory.Height - _item.Height));
        }

        _selector.AssignItem(_item);
        SetItemTileAppearance();

        GD.Print("Took item (transport)");
    }

    public void RevertTakeItem() {
        if (_item == null || _lastTake == null) {
            return;
        }

        InventoryInstance.Mutator? mutator = _lastTake.FromMutator;
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
        if (_currentInventory != null) {
            SetItemTileAppearance();
        }
    }

    public void RotateCounterClockwise() {
        if (_item == null) {
            return;
        }
        _item.RotateCounterClockwise();
        _selector.OnItemUpdated();
        if (_currentInventory != null) {
            SetItemTileAppearance();
        }
    }

    private void ClearItem() {
        _item = null;
        _selector.UnassignItem();
        if (_currentInventory != null) {
            _currentInventory.Frame.ClearItemTilesAppearance();
            // bounds need to be set for each frame to avoid edge conditions when transporting items between inventories
            foreach (OpenedInventory openedInventory in _openedInventories.Values) {
                openedInventory.Frame.ResetSelectionBound();
            }
        }
    }

    public void OnSelectedPositionChanged(string inventoryInstanceID, Vector2I tilePosition) {
        if (_currentInventory == null) {
            throw new Exception("Cannot change selected position because current inventory is null.");
        }
        if (_currentInventory.Inventory.InventoryInstanceID != inventoryInstanceID) {
            return;
        }
        TilePosition = tilePosition;
        _selector.GlobalPosition = _currentInventory.Frame.GetSelectorGlobalPosition();
        SetItemTileAppearance();
    }

    public void SetItemTileAppearance() {
        if (_item == null || _currentInventory == null) {
            return;
        }
        _item.X = TilePosition.X;
        _item.Y = TilePosition.Y;
        _currentInventory.Frame.ClearItemTilesAppearance();
        _currentInventory.Frame.SetItemTilesAppearance(_item);
    }

    public void OnInventoryFocused(string inventoryInstanceID) {
        if (_currentInventory != null) {
            _currentInventory.Frame.ClearItemTilesAppearance();
        }
        _currentInventory = _openedInventories[inventoryInstanceID];
        if (_currentInventory == null) {
            throw new Exception("Failed to get inventory from focus");
        }

        if (_playerContext == null) {
            throw new Exception("PlayerContext null");
        }
        InputType inputType = _playerContext.Controller.InputType;
        if (inputType == InputType.KeyboardMouse) {
            TilePosition = _currentInventory.Frame.SelectNearestTile(_selector.GlobalPosition);
        }
        else if (inputType == InputType.Joypad) {
            TilePosition = _currentInventory.Frame.SelectedPosition;
        }

        _selector.GlobalPosition = _currentInventory.Frame.GetSelectorGlobalPosition();
        _selector.SetHoveringInventory(true);
        SetItemTileAppearance();
    }

    public void OnInventoryUnfocused(string inventoryInstanceID) {
        if (_currentInventory == null) {
            return;
        }
        if (inventoryInstanceID == _currentInventory.Inventory.InventoryInstanceID) {
            _selector.SetHoveringInventory(false);
            _currentInventory.Frame.ClearItemTilesAppearance();
            _currentInventory = null;
        }
    }

    public void OnControllerInputTypeChanged(InputType inputType) {
        if (_currentInventory == null && inputType == InputType.Joypad) {
            if (_openedInventories.Count == 0) {
                return;
            }
            if (_currentInventory == null) {
                SelectNextInventoryFrame();
            }
        }
    }

    public void SelectInventoryFrame(string inventoryInstanceID) {
        if (!_openedInventories.ContainsKey(inventoryInstanceID)) {
            throw new Exception("Failed to select inventory frame because it's not open.");
        }
        if (_currentInventory != null) {
            _currentInventory.Frame.ClearItemTilesAppearance();
        }
        _currentInventory = _openedInventories[inventoryInstanceID];
        _currentInventory.Frame.GrabPseudoFocus();
    }

    public void SelectNextInventoryFrame() {
        if (_playerContext == null) {
            throw new Exception("PlayerContext null");
        }
        if (_playerContext.Controller.InputType != InputType.Joypad) {
            return;
        }
        if (_openedInventories.Count == 0) {
            return;
        }
        if (_currentInventory == null) {
            _currentInventory = _openedInventories.First().Value;
            _currentInventory.Frame.GrabPseudoFocus();
            return;
        }
        int currentIndex = _openedInventoryOrder.IndexOf(_currentInventory.Inventory.InventoryInstanceID);
        if (currentIndex == -1) {
            throw new Exception("Failed to find current inventory in opened inventory order.");
        }
        int nextIndex = (currentIndex + 1) % _openedInventoryOrder.Count;
        _currentInventory.Frame.ClearItemTilesAppearance();
        _currentInventory = _openedInventories[_openedInventoryOrder[nextIndex]];
        _currentInventory.Frame.GrabPseudoFocus();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class InventoryDTO : IGameAssetDTO {
    public int Width { get; set; }
    public int Height { get; set; }
    public InventoryItemInstanceDTO[]? Items { get; set; }
    public string? BackgroundImagePath { get; set; }
    // disabled is used when an inventory needs to exist, but shouldn't be interactive
    // - inspecting the contents of a locked chest
    // - displaying a completed crafting result
    public bool Disabled { get; set; }
    public bool[]? UsableMask { get; set; }

    public bool IsValid() {
        if (Width <= 0 || Height <= 0) {
            return false;
        }
        if (UsableMask != null) {
            if (UsableMask.Length != Width * Height) {
                return false;
            }
            if (!ConnectedArray.IsArrayConnected(Width, Height, UsableMask)) {
                return false;
            }
        }

        return true;
    }

    public string Stringify() {
        string str = $"Width: {Width}\nHeight: {Height}\n";
        if (BackgroundImagePath != null) {
            str += $"BackgroundImagePath: {BackgroundImagePath}\n";
        }
        str += $"Disabled: {Disabled}\n";
        if (UsableMask != null) {
            str += "UsableMask:\n";
            for (int y = 0; y < Height; ++y) {
                for (int x = 0; x < Width; ++x) {
                    str += UsableMask[y * Width + x] ? "1" : "0";
                }
                str += "\n";
            }
        }
        if (Items != null) {
            str += "Items (array):\n";
            foreach (InventoryItemInstanceDTO item in Items) {
                str += $"{item.Stringify()}\n\n";
            }
        }
        return str;
    }
}

public partial class Inventory : Node {
    public int Width { get; set; }
    public int Height { get; set; }
    public List<InventoryItemInstance> Items { get; set; }
    public string? BackgroundImagePath { get; set; }
    public bool Disabled { get; set; }

    public bool Touched { get; set; }
    // indicates if mutations should cause the inventory to be touched (e.g. ignore initialization placements)
    private bool _ignoreTouches = false;

    // indicate spaces that are usable (shape might not be a perfect rectangle)
    private bool[] _usableMask;
    // spaces that are currently occupied
    // each space contains the index of the item occupying it or UNUSED_SPACE_PLACEHOLDER
    private int[] _itemMask;
    // TODO: Complete() and add masks for specific categories and item UUIDs
    // _locked is used to prevent creating multiple mutators at once
    private bool _locked;
    private bool _printLogs = true;

    private const int UNUSED_SPACE_PLACEHOLDER = -1;

    public enum UpdateType {
        Place = 0,
        Take = 2,
    }
    [Signal]
    public delegate void UpdatedEventHandler(UpdateType updateType, string itemInstanceID);

    public Inventory(InventoryDTO dto) {
        if (!dto.IsValid()) {
            throw new ArgumentException("Invalid InventoryDTO");
        }
        Width = dto.Width;
        Height = dto.Height;
        BackgroundImagePath = dto.BackgroundImagePath;
        Disabled = dto.Disabled;
        if (dto.UsableMask == null) {
            _usableMask = Enumerable.Repeat(true, Width * Height).ToArray();
        }
        else {
            _usableMask = dto.UsableMask;
        }
        _itemMask = Enumerable.Repeat(UNUSED_SPACE_PLACEHOLDER, Width * Height).ToArray();

        Items = new List<InventoryItemInstance>();
        if (dto.Items != null) {
            if (_printLogs) {
                GD.Print($"Placing {dto.Items.Length} items into inventory");
            }
            _ignoreTouches = true;
            foreach (InventoryItemInstanceDTO item in dto.Items) {
                InventoryItemInstance itemInstance = new InventoryItemInstance(item);
                PlaceItem(itemInstance, item.X, item.Y);
            }
            _ignoreTouches = false;
        }
    }

    public Inventory(int width, int height) {
        Width = width;
        Height = height;
        _usableMask = Enumerable.Repeat(true, Width * Height).ToArray();
        _itemMask = Enumerable.Repeat(UNUSED_SPACE_PLACEHOLDER, Width * Height).ToArray();
        Items = new List<InventoryItemInstance>();
    }

    public bool CanPlaceItem(InventoryItemInstance item, int x, int y) {
        InventoryItemDefinition itemDef = AssetManager.Ref().GetInventoryItemDefinition(item.ItemDefinitionID);
        if (x < 0 || y < 0 || x + itemDef.Space.Width > Width || y + itemDef.Space.Height > Height) {
            if (_printLogs) {
                GD.Print($"Can't place item {item.ItemDefinitionID} at ({x}, {y}) due to edge of inventory");
            }
            return false;
        }
        for (int i = 0; i < itemDef.Space.Width; i++) {
            for (int j = 0; j < itemDef.Space.Height; j++) {
                int indexConsidered = (y + j) * Width + (x + i);
                if (!itemDef.Space.GetFilledMask(item.Rotation)[j * itemDef.Space.Width + i]) {
                    continue;
                }
                if (!_usableMask[indexConsidered]) {
                    if (_printLogs) {
                        GD.Print($"Can't place item {item.ItemDefinitionID} at ({x}, {y}) due to unusable space");
                    }
                    return false;
                }
                if (_itemMask[indexConsidered] != UNUSED_SPACE_PLACEHOLDER) {
                    if (_printLogs) {
                        GD.Print($"Can't place item {item.ItemDefinitionID} at ({x}, {y}) due to occupied space");
                    }
                    return false;
                }
            }
        }
        return true;
    }

    public void UpdateItemMask() {
        int[] newItemMask = Enumerable.Repeat(UNUSED_SPACE_PLACEHOLDER, Width * Height).ToArray();
        for (int idx = 0; idx < Items.Count; idx++) {
            InventoryItemDefinition itemDef;
            itemDef = AssetManager.Ref().GetInventoryItemDefinition(Items[idx].ItemDefinitionID);

            for (int i = 0; i < itemDef.Space.Width; i++) {
                for (int j = 0; j < itemDef.Space.Height; j++) {
                    if (!itemDef.Space.GetFilledMask(Items[idx].Rotation)[j * itemDef.Space.Width + i]) {
                        continue;
                    }
                    int indexConsidered = (Items[idx].Y + j) * Width + (Items[idx].X + i);
                    DebugTools.Assert(_usableMask[indexConsidered]);
                    DebugTools.Assert(newItemMask[indexConsidered] == UNUSED_SPACE_PLACEHOLDER);
                    newItemMask[indexConsidered] = idx;
                }
            }
        }
        _itemMask = newItemMask;
    }

    public bool SpaceFilled(int x, int y) {
        return _itemMask[y * Width + x] != UNUSED_SPACE_PLACEHOLDER;
    }

    public bool SpaceUsable(int x, int y) {
        if (x < 0 || y < 0 || x >= Width || y >= Height) {
            return false;
        }
        return _usableMask[y * Width + x];
    }

    public InventoryItemInstance? ItemAt(int x, int y) {
        int index = _itemMask[y * Width + x];
        if (index == UNUSED_SPACE_PLACEHOLDER) {
            return null;
        }
        return Items[index];
    }

    private bool PlaceItem(InventoryItemInstance item, int x, int y) {
        if (!CanPlaceItem(item, x, y)) {
            if (_printLogs) {
                GD.Print($"Can't place item {item.ItemDefinitionID} at ({x}, {y}) (CanPlaceItem() failed)");
            }
            return false;
        }
        item.X = x;
        item.Y = y;
        Items.Add(item);
        UpdateItemMask();
        if (!_ignoreTouches) {
            Touched = true;
            EmitSignal(SignalName.Updated, (int)UpdateType.Place, item.ItemInstanceID);
        }
        if (_printLogs) {
            GD.Print($"Placed item {item.ItemInstanceID} with definition {item.ItemDefinitionID} at ({x}, {y})");
        }
        return true;
    }

    public InventoryItemInstance? GetItemByID(string itemInstanceID) {
        foreach (InventoryItemInstance item in Items) {
            if (item.ItemInstanceID == itemInstanceID) {
                return item;
            }
        }
        return null;
    }

    private InventoryItemInstance? TakeItem(int x, int y) {
        InventoryItemInstance? item = ItemAt(x, y);
        if (item == null) {
            if (_printLogs) {
                GD.Print($"Can't take item at ({x}, {y}) (no item there)");
            }
            return null;
        }
        bool removeSucceeded = Items.Remove(item);
        if (!removeSucceeded) {
            throw new Exception("Failed to remove item from inventory");
        }
        UpdateItemMask();
        if (!_ignoreTouches) {
            Touched = true;
            EmitSignal(SignalName.Updated, (int)UpdateType.Take, item.ItemInstanceID);
        }
        if (_printLogs) {
            GD.Print($"Took item {item.ItemInstanceID} with definition {item.ItemDefinitionID} at ({x}, {y})");
        }
        return item;
    }

    public Mutator? GetMutator() {
        if (Disabled || _locked) {
            return null;
        }
        _locked = true;
        return new Mutator(this);
    }

    public class Mutator : IDisposable {
        private Inventory _inventory;
        private bool _released;
        public Mutator(Inventory inventory) {
            _inventory = inventory;
            _released = false;
        }
        ~Mutator() {
            if (!_released) {
                GD.PrintErr("InventoryMutator was not disposed before destructor.");
                Dispose();
            }
        }

        public void Dispose() {
            // should be idempotent
            if (!_released) {
                _released = true;
                _inventory._locked = false;
            }
        }

        public bool PlaceItem(InventoryItemInstance item, int x, int y) {
            if (_released) {
                GD.PrintErr("InventoryMutator.PlaceItem called after mutator was disposed");
                return false;
            }
            return _inventory.PlaceItem(item, x, y);
        }

        public InventoryItemInstance? TakeItem(int x, int y) {
            if (_released) {
                GD.PrintErr("InventoryMutator.TakeItem called after mutator was disposed");
                return null;
            }
            return _inventory.TakeItem(x, y);
        }
    }

    public string StringRepresentationOfGrid() {
        string str = "";
        for (int y = 0; y < Height; ++y) {
            for (int x = 0; x < Width; ++x) {
                if (!_usableMask[y * Width + x]) {
                    str += "XX";
                    continue;
                }
                if (_itemMask[y * Width + x] == UNUSED_SPACE_PLACEHOLDER) {
                    str += "  ";
                    continue;
                }
                str += $"{_itemMask[y * Width + x]:D2}";
            }
            str += "\n";
        }
        return str;
    }
}

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

public class Inventory {
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
    // each space contains the index of the item occupying it. -1 if empty
    private int[] _itemMask;
    // TODO: Complete() and add masks for specific categories and item UUIDs
    // _locked is used to prevent creating multiple mutators at once
    private bool _locked;
    private bool _printLogs = true;

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
        _itemMask = Enumerable.Repeat(-1, Width * Height).ToArray();

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
        _itemMask = Enumerable.Repeat(-1, Width * Height).ToArray();
        Items = new List<InventoryItemInstance>();
    }

    public bool CanPlaceItem(InventoryItemInstance item, int x, int y) {
        InventoryItemDefinition itemDef = AssetManager.Ref().GetInventoryItemDefinition(item.DefinitionID);
        if (x < 0 || y < 0 || x + itemDef.Space.Width > Width || y + itemDef.Space.Height > Height) {
            if (_printLogs) {
                GD.Print($"Can't place item {item.DefinitionID} at ({x}, {y}) due to edge of inventory");
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
                        GD.Print($"Can't place item {item.DefinitionID} at ({x}, {y}) due to unusable space");
                    }
                    return false;
                }
                if (_itemMask[indexConsidered] != -1) {
                    if (_printLogs) {
                        GD.Print($"Can't place item {item.DefinitionID} at ({x}, {y}) due to occupied space");
                    }
                    return false;
                }
            }
        }
        return true;
    }

    public void UpdateItemMask() {
        int[] newItemMask = Enumerable.Repeat(-1, Width * Height).ToArray();
        for (int idx = 0; idx < Items.Count; idx++) {
            InventoryItemDefinition itemDef;
            itemDef = AssetManager.Ref().GetInventoryItemDefinition(Items[idx].DefinitionID);

            for (int i = 0; i < itemDef.Space.Width; i++) {
                for (int j = 0; j < itemDef.Space.Height; j++) {
                    if (!itemDef.Space.GetFilledMask(Items[idx].Rotation)[j * itemDef.Space.Width + i]) {
                        continue;
                    }
                    int indexConsidered = (Items[idx].Y + j) * Width + (Items[idx].X + i);
                    DebugTools.Assert(_usableMask[indexConsidered]);
                    DebugTools.Assert(newItemMask[indexConsidered] == -1);
                    newItemMask[indexConsidered] = idx;
                }
            }
        }
        _itemMask = newItemMask;
    }

    public bool SpaceUsable(int x, int y) {
        if (x < 0 || y < 0 || x >= Width || y >= Height) {
            return false;
        }
        return _usableMask[y * Width + x];
    }

    public InventoryItemInstance? ItemAt(int x, int y) {
        int index = _itemMask[y * Width + x];
        if (index == -1) {
            return null;
        }
        return Items[index];
    }

    private bool PlaceItem(InventoryItemInstance item, int x, int y) {
        if (!CanPlaceItem(item, x, y)) {
            if (_printLogs) {
                GD.Print($"Can't place item {item.DefinitionID} at ({x}, {y}) (CanPlaceItem() failed)");
            }
            return false;
        }
        item.X = x;
        item.Y = y;
        Items.Add(item);
        UpdateItemMask();
        if (!_ignoreTouches) {
            Touched = true;
        }
        if (_printLogs) {
            GD.Print($"Placed item {item.DefinitionID} at ({x}, {y})");
        }
        return true;
    }

    private InventoryItemInstance? TakeItem(int x, int y) {
        InventoryItemInstance? item = ItemAt(x, y);
        if (item == null) {
            return null;
        }
        Items.Remove(item);
        UpdateItemMask();
        if (!_ignoreTouches) {
            Touched = true;
        }
        if (_printLogs) {
            GD.Print($"Took item {item.DefinitionID} at ({x}, {y})");
        }
        return item;
    }

    public InventoryMutator? GetMutator() {
        if (Disabled || _locked) {
            return null;
        }
        _locked = true;
        return new InventoryMutator(this);
    }

    public class InventoryMutator {
        private Inventory _inventory;
        private bool _released;
        public InventoryMutator(Inventory inventory) {
            _inventory = inventory;
            _released = false;
        }
        ~InventoryMutator() {
            DebugTools.Assert(_inventory._locked, "Inventory was not locked when mutator was destroyed.");
            _inventory._locked = false;
        }

        public void Release() {
            DebugTools.Assert(!_released, "InventoryMutator.Release called multiple times");
            if (!_released) {
                _released = true;
                _inventory._locked = false;
            }
        }

        public bool PlaceItem(InventoryItemInstance item, int x, int y) {
            if (_released) {
                GD.PrintErr("InventoryMutator.PlaceItem called after InventoryMutator.Release");
                return false;
            }
            return _inventory.PlaceItem(item, x, y);
        }

        public InventoryItemInstance? TakeItem(int x, int y) {
            if (_released) {
                GD.PrintErr("InventoryMutator.TakeItem called after InventoryMutator.Release");
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
                if (_itemMask[y * Width + x] == -1) {
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

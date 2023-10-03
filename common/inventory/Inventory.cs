using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class InventoryDTO : IGameAssetDTO {
    public int Width { get; set; }
    public int Height { get; set; }
    public List<InventoryItemInstanceDTO>? Items { get; set; }
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
            str += "Items:\n";
            foreach (InventoryItemInstanceDTO item in Items) {
                str += $"{item.Stringify()}\n";
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
    // hint to the game that this inventory should be cleared when closed (e.g. for fishing result / temporary inventories)
    // TODO: this should throw an error somehow if the inventory contains items that cannot be deleted / are quest items
    public bool ShouldClearOnClose { get; set; }
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
            _ignoreTouches = true;
            foreach (InventoryItemInstance item in Items) {
                PlaceItem(item, item.X, item.Y);
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
            return false;
        }
        for (int i = 0; i < itemDef.Space.Width; i++) {
            for (int j = 0; j < itemDef.Space.Height; j++) {
                int indexConsidered = (y + j) * Width + (x + i);
                if (!_usableMask[indexConsidered]) {
                    return false;
                }
                if (_itemMask[indexConsidered] != -1) {
                    return false;
                }
            }
        }
        return true;
    }

    public void UpdateItemMask() {
        int[] newItemMask = Enumerable.Repeat(-1, Width * Height).ToArray();
        for (int i = 0; i < Items.Count; i++) {
            InventoryItemDefinition itemDef;
            itemDef = AssetManager.Ref().GetInventoryItemDefinition(Items[i].DefinitionID);

            for (int x = 0; x < itemDef.Space.Width; x++) {
                for (int y = 0; y < itemDef.Space.Height; y++) {
                    int indexConsidered = (Items[i].Y + y) * Width + (Items[i].X + x);
                    DebugTools.Assert(_usableMask[indexConsidered]);
                    DebugTools.Assert(newItemMask[indexConsidered] == -1);
                    newItemMask[indexConsidered] = i;
                }
            }
        }
        _itemMask = newItemMask;
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
            return false;
        }
        item.X = x;
        item.Y = y;
        UpdateItemMask();
        if (!_ignoreTouches) {
            Touched = true;
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
}

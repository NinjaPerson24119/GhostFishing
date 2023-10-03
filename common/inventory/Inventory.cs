using System;
using System.Collections.Generic;
using System.Linq;

public class InventoryDTO : IGameAssetDTO {
    public int Width { get; set; }
    public int Height { get; set; }
    public List<InventoryItemInstanceDTO>? Items { get; set; }
    public string? BackgroundImagePath { get; set; }
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
    private bool[] _filledMask;
    // TODO: Complete() and add masks for specific categories and item UUIDs

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
        _filledMask = new bool[Width * Height];

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
        _filledMask = Enumerable.Repeat(true, Width * Height).ToArray();
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
                if (_filledMask[indexConsidered]) {
                    return false;
                }
            }
        }
        return true;
    }

    public bool[] GenerateFilledMask() {
        bool[] newFilledMask = new bool[Width * Height];
        foreach (InventoryItemInstance item in Items) {
            InventoryItemDefinition itemDef;
            itemDef = AssetManager.Ref().GetInventoryItemDefinition(item.DefinitionID);

            for (int i = 0; i < itemDef.Space.Width; i++) {
                for (int j = 0; j < itemDef.Space.Height; j++) {
                    int indexConsidered = (item.Y + j) * Width + (item.X + i);
                    DebugTools.Assert(_usableMask[indexConsidered]);
                    DebugTools.Assert(!newFilledMask[indexConsidered]);
                    newFilledMask[indexConsidered] = true;
                }
            }
        }
        return newFilledMask;
    }

    public bool PlaceItem(InventoryItemInstance item, int x, int y) {
        if (!CanPlaceItem(item, x, y)) {
            return false;
        }
        item.X = x;
        item.Y = y;
        _filledMask = GenerateFilledMask();
        if (!_ignoreTouches) {
            Touched = true;
        }
        return true;
    }
}

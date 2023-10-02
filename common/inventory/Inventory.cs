using System.Text.Json.Serialization;
using System.Collections.Generic;
using Godot;

public class Inventory : IValidatedGameAsset {
    public string Name { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public List<InventoryItemInstance> Items { get; set; }
    public string BackgroundImagePath { get; set; }
    // hint to the game that this inventory should be cleared when closed (e.g. for fishing result / temporary inventories)
    // TODO: this should throw an error somehow if the inventory contains items that cannot be deleted / are quest items
    public bool ShouldClearOnClose { get; set; }
    public bool Disabled { get; set; }

    public bool Touched { get; set; }
    // indicates if mutations should cause the inventory to be touched (e.g. ignore initialization placements)
    private bool _ignoreTouches = false;

    // indicate spaces that are usable (shape might not be a perfect rectangle)
    [JsonPropertyName("UsableMask")]
    public bool[] SerializedUsableMask {
        get {
            return _usableMask;
        }
        set {
            _usableMask = value;
        }
    }
    private bool[] _usableMask;
    // spaces that are currently occupied
    private bool[] _filledMask;
    // TODO: Complete() and add masks for specific categories and item UUIDs

    public Inventory() {
        Items = new List<InventoryItemInstance>();
    }

    public void Initialize() {
        if (Items != null) {
            _ignoreTouches = true;
            foreach (InventoryItemInstance item in Items) {
                PlaceItem(item, item.X, item.Y);
            }
            _ignoreTouches = false;
        }
    }

    public bool CanPlaceItem(InventoryItemInstance item, int x, int y) {
        InventoryItemDefinition itemDef;
        try {
            itemDef = AssetManager.Ref().GetInventoryItemDefinition(item.DefinitionID);
        }
        catch {
            return false;
        }
        if (itemDef.Space == null) {
            GD.PrintErr($"Inventory item definition {item.DefinitionID} has no space defined");
            return false;
        }

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
        if (Items == null) {
            return newFilledMask;
        }
        foreach (InventoryItemInstance item in Items) {
            InventoryItemDefinition itemDef;
            try {
                itemDef = AssetManager.Ref().GetInventoryItemDefinition(item.DefinitionID);
            }
            catch {
                continue;
            }
            if (itemDef.Space == null) {
                GD.PrintErr($"Inventory item definition {item.DefinitionID} has no space defined");
                continue;
            }

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
        InventoryItemDefinition itemDef;
        try {
            itemDef = AssetManager.Ref().GetInventoryItemDefinition(item.DefinitionID);
        }
        catch {
            return false;
        }
        if (itemDef.Space == null) {
            GD.PrintErr($"Inventory item definition {item.DefinitionID} has no space defined");
            return false;
        }

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

    public bool Validate() {
        if (Name.Length == 0) {
            return false;
        }
        if (Width <= 0 || Height <= 0) {
            return false;
        }
        if (SerializedUsableMask == null) {
            return false;
        }
        if (SerializedUsableMask.Length != Width * Height) {
            return false;
        }
        if (!ConnectedArray.IsArrayConnected(Width, Height, SerializedUsableMask)) {
            return false;
        }
        return true;
    }

    public string Stringify() {
        string str = $"Name: {Name}\nWidth: {Width}\nHeight: {Height}\n";
        str += $"BackgroundImagePath: {BackgroundImagePath}\n";
        str += $"ShouldClearOnClose: {ShouldClearOnClose}\n";
        str += $"Disabled: {Disabled}\n";
        str += $"Touched: {Touched}\n";
        if (SerializedUsableMask != null) {
            str += "UsableMask:\n";
            for (int y = 0; y < Height; ++y) {
                for (int x = 0; x < Width; ++x) {
                    str += SerializedUsableMask[y * Width + x] ? "1" : "0";
                }
                str += "\n";
            }
        }
        if (Items != null) {
            str += "Items:\n";
            foreach (InventoryItemInstance item in Items) {
                str += $"{item.Stringify()}\n";
            }
        }
        return str;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class InventoryInstanceDTO : IGameAssetDTO {
    public string? InventoryDefinitionID { get; set; }
    public InventoryItemInstanceDTO[]? Items { get; set; }
    public bool Disabled { get; set; }

    public bool IsValid() {
        if (string.IsNullOrEmpty(InventoryDefinitionID) || !AssetIDUtil.IsInventoryDefinitionID(InventoryDefinitionID)) {
            return false;
        }
        return true;
    }

    public string Stringify() {
        string str = $"InventoryDefinitionID: {InventoryDefinitionID}\n";
        str += $"Disabled: {Disabled}\n";
        if (Items != null) {
            str += "Items (array):\n";
            foreach (InventoryItemInstanceDTO item in Items) {
                str += $"{item.Stringify()}\n\n";
            }
        }
        return str;
    }
}

// this is only a Node because we want to use Signals, don't try to add it to the tree
public partial class InventoryInstance : Node, IGameAssetWritable<InventoryInstanceDTO> {
    public readonly string InventoryInstanceID;
    private readonly string? _inventoryDefinitionID;

    private InventoryDefinition _definition;
    public int Width { get => _definition.Width; }
    public int Height { get => _definition.Height; }
    public string? BackgroundImagePath { get => _definition.BackgroundImagePath; }

    public List<InventoryItemInstance> Items { get; set; }
    public bool Disabled { get; set; }
    public bool Touched { get; set; }

    // indicates if mutations should cause the inventory to be touched (e.g. ignore initialization placements)
    private bool _ignoreTouches = false;

    // spaces that are currently occupied
    // each space contains the index of the item occupying it or UNUSED_SPACE_PLACEHOLDER
    private int[] _itemMask;

    // TODO: Complete() and add masks for specific categories and item UUIDs
    // _locked is used to prevent creating multiple mutators at once
    public bool Locked { get; private set; }
    private bool _printLogs = true;

    private const int UNUSED_SPACE_PLACEHOLDER = -1;

    public enum UpdateType {
        Place = 0,
        Take = 2,
    }
    [Signal]
    public delegate void UpdatedEventHandler(UpdateType updateType, string itemInstanceID);

    // e.g. for temporary inventory
    public InventoryInstance(int width, int height) {
        InventoryInstanceID = AssetIDUtil.GenerateInventoryInstanceID();
        _inventoryDefinitionID = null;
        _definition = new InventoryDefinition(width, height);
        _itemMask = Enumerable.Repeat(UNUSED_SPACE_PLACEHOLDER, Width * Height).ToArray();
        Items = new List<InventoryItemInstance>();
    }

    public InventoryInstance(string id, InventoryInstanceDTO dto) {
        if (!dto.IsValid()) {
            throw new ArgumentException("Invalid InventoryInstanceDTO");
        }
        if (!AssetIDUtil.IsInventoryInstanceID(id)) {
            throw new ArgumentException($"Invalid InventoryInstanceID: {id}");
        }
        InventoryInstanceID = id;
        _inventoryDefinitionID = dto.InventoryDefinitionID;
        _definition = AssetManager.Ref().GetInventoryDefinition(dto.InventoryDefinitionID!);

        Disabled = dto.Disabled;
        _itemMask = Enumerable.Repeat(UNUSED_SPACE_PLACEHOLDER, Width * Height).ToArray();

        Items = new List<InventoryItemInstance>();
        if (dto.Items != null) {
            if (_printLogs) {
                GD.Print($"Placing {dto.Items.Length} items into inventory");
            }
            _ignoreTouches = true;
            foreach (InventoryItemInstanceDTO item in dto.Items) {
                InventoryItemInstance itemInstance = new InventoryItemInstance(item);
                bool success = PlaceItem(itemInstance);
                if (!success) {
                    GD.PrintErr($"Constructor: Failed to place item into inventory (InventoryInstanceID: {InventoryInstanceID}, ItemInstanceID: {itemInstance.ItemInstanceID})");
                }
            }
            _ignoreTouches = false;
        }
    }

    public bool CanPlaceItem(InventoryItemInstance item) {
        int x = item.X;
        int y = item.Y;
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
                if (!_definition.UsableMask[indexConsidered]) {
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
                    DebugTools.Assert(_definition.UsableMask[indexConsidered]);
                    DebugTools.Assert(newItemMask[indexConsidered] == UNUSED_SPACE_PLACEHOLDER);
                    newItemMask[indexConsidered] = idx;
                }
            }
        }
        _itemMask = newItemMask;
    }

    public bool SpaceFilled(int x, int y) {
        if (x < 0 || y < 0 || x >= Width || y >= Height) {
            return false;
        }
        if (!SpaceUsable(x, y)) {
            return false;
        }
        return _itemMask[y * Width + x] != UNUSED_SPACE_PLACEHOLDER;
    }

    public bool SpaceUsable(int x, int y) {
        if (x < 0 || y < 0 || x >= Width || y >= Height) {
            return false;
        }
        return _definition.UsableMask[y * Width + x];
    }

    public InventoryItemInstance? ItemAt(int x, int y) {
        int index = _itemMask[y * Width + x];
        if (index == UNUSED_SPACE_PLACEHOLDER) {
            return null;
        }
        return Items[index];
    }

    private bool PlaceItem(InventoryItemInstance item) {
        if (!CanPlaceItem(item)) {
            if (_printLogs) {
                GD.Print($"Can't place item {item.ItemDefinitionID} at ({item.X}, {item.Y}) (CanPlaceItem() failed)");
            }
            return false;
        }
        Items.Add(item);
        UpdateItemMask();
        if (!_ignoreTouches) {
            Touched = true;
            EmitSignal(SignalName.Updated, (int)UpdateType.Place, item.ItemInstanceID);
        }
        if (_printLogs) {
            GD.Print($"Placed item {item.ItemInstanceID} with definition {item.ItemDefinitionID} at ({item.X}, {item.Y})");
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
        if (Disabled || Locked) {
            return null;
        }
        Locked = true;
        return new Mutator(this);
    }

    public class Mutator : IDisposable {
        private InventoryInstance _inventory;
        private bool _released;
        public Mutator(InventoryInstance inventory) {
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
                _inventory.Locked = false;
            }
        }

        public bool PlaceItem(InventoryItemInstance item) {
            if (_released) {
                GD.PrintErr("InventoryMutator.PlaceItem called after mutator was disposed");
                return false;
            }
            return _inventory.PlaceItem(item);
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
                if (!_definition.UsableMask[y * Width + x]) {
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

    public InventoryInstanceDTO ToDTO() {
        if (string.IsNullOrEmpty(_inventoryDefinitionID)) {
            throw new Exception("Can't write inventory instance without a corresponding definition. Likely tried to write a temporary inventory instance.");
        }

        InventoryInstanceDTO dto = new InventoryInstanceDTO() {
            InventoryDefinitionID = _inventoryDefinitionID,
            Disabled = Disabled,
            Items = new InventoryItemInstanceDTO[Items.Count],
        };
        for (int i = 0; i < Items.Count; ++i) {
            dto.Items[i] = Items[i].ToDTO();
        }
        return dto;
    }

    public bool IsTouched() {
        // we can't write inventory instances without a corresponding definition
        return Touched && !string.IsNullOrEmpty(_inventoryDefinitionID);
    }

    public new void QueueFree() {
        throw new Exception("InventoryInstance.QueueFree() is not supported. It's a Node only so we can use signals.");
    }
}

using System;
using Godot;

internal class InventoryItemInstanceDTO : IGameAssetDTO {
    public string? ItemInstanceID { get; set; }
    public string? ItemDefinitionID { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public InventoryItemRotation Rotation { get; set; }
    public InventoryItemFlagsDTO? FlagOverrides { get; set; }

    public bool IsValid() {
        if (string.IsNullOrEmpty(ItemDefinitionID)) {
            return false;
        }
        if (X < 0 || Y < 0) {
            return false;
        }
        return true;
    }

    public string Stringify() {
        string str = $"ItemInstanceID (this may be empty until DTO is instanced): {ItemInstanceID}\n";
        str += $"ItemDefinitionID: {ItemDefinitionID}\n";
        str += $"X: {X}, Y: {Y}, Rotation: {Rotation}\n";
        if (FlagOverrides != null) {
            str += $"FlagOverrides (object):\n{FlagOverrides.Stringify()}\n";
        }
        return str;
    }
}

internal class InventoryItemInstance {
    public string ItemInstanceID { get; set; }
    public string ItemDefinitionID { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public InventoryItemRotation Rotation { get; set; }
    public float RotationRadians {
        get {
            return Mathf.Pi / 2 * (int)Rotation;
        }
    }
    public InventoryItemFlags? FlagOverrides { get; set; }
    public Color? BackgroundColor;

    public InventoryItemInstance(InventoryItemInstanceDTO dto) {
        if (!dto.IsValid()) {
            throw new ArgumentException("Invalid InventoryItemInstanceDTO");
        }
        if (string.IsNullOrEmpty(dto.ItemInstanceID)) {
            ItemInstanceID = AssetIDUtil.GenerateInventoryItemInstanceID();
        }
        else {
            ItemInstanceID = dto.ItemInstanceID;
        }
        ItemDefinitionID = dto.ItemDefinitionID!;
        X = dto.X;
        Y = dto.Y;
        Rotation = dto.Rotation;
        if (dto.FlagOverrides != null) {
            FlagOverrides = new InventoryItemFlags(dto.FlagOverrides);
        }

        InventoryItemDefinition itemDef = AssetManager.Ref().GetInventoryItemDefinition(ItemDefinitionID);
        if (itemDef.BackgroundColorOverride != null) {
            BackgroundColor = itemDef.BackgroundColorOverride;
        }
        else if (itemDef.CategoryID != null) {
            InventoryItemCategory category = AssetManager.Ref().GetInventoryCategory(itemDef.CategoryID);
            BackgroundColor = category.BackgroundColor;
        }
    }

    public void RotateClockwise() {
        Rotation = (InventoryItemRotation)(((int)Rotation + 1) % 4);
    }

    public void RotateCounterClockwise() {
        Rotation = (InventoryItemRotation)(((int)Rotation + 3) % 4);
    }
}

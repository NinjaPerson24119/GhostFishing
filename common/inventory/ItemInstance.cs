using System;
using Godot;

internal class InventoryItemInstanceDTO : IGameAssetDTO {
    public string? ItemInstanceID { get; set; }
    public string? ItemDefinitionID { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public InventoryItemRotation Rotation { get; set; }

    public bool IsValid() {
        if (string.IsNullOrEmpty(ItemDefinitionID) || !AssetIDUtil.IsInventoryItemDefinitionID(ItemDefinitionID)) {
            GD.PrintErr($"Invalid ItemDefinitionID: {ItemDefinitionID}");
            return false;
        }
        if (!string.IsNullOrEmpty(ItemInstanceID) && !AssetIDUtil.IsInventoryItemInstanceID(ItemInstanceID)) {
            GD.PrintErr($"Invalid ItemInstanceID: {ItemInstanceID}");
            return false;
        }
        if (X < 0 || Y < 0) {
            GD.PrintErr($"Invalid position: {X}, {Y}");
            return false;
        }
        return true;
    }

    public string Stringify() {
        // items need not specify an ID as they don't exist outside of inventories
        // it will be generated when the item is added to an inventory
        string str = $"ItemInstanceID (this may be empty until DTO is instanced): {ItemInstanceID}\n";
        str += $"ItemDefinitionID: {ItemDefinitionID}\n";
        str += $"X: {X}, Y: {Y}, Rotation: {Rotation}\n";
        return str;
    }
}

internal class InventoryItemInstance : IGameAssetWritable<InventoryItemInstanceDTO> {
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

    public InventoryItemInstanceDTO ToDTO() {
        InventoryItemInstanceDTO dto = new InventoryItemInstanceDTO() {
            ItemInstanceID = ItemInstanceID,
            ItemDefinitionID = ItemDefinitionID,
            X = X,
            Y = Y,
            Rotation = Rotation
        };
        return dto;
    }

    public bool IsTouched() {
        return true;
    }
}

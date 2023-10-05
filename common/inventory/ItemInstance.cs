using System;
using Godot;

public class InventoryItemInstanceDTO : IGameAssetDTO {
    public string? InstanceID { get; set; }
    public string? DefinitionID { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public InventoryItemRotation Rotation { get; set; }
    public InventoryItemFlagsDTO? FlagOverrides { get; set; }

    public bool IsValid() {
        if (string.IsNullOrEmpty(DefinitionID)) {
            return false;
        }
        if (X < 0 || Y < 0) {
            return false;
        }
        return true;
    }

    public string Stringify() {
        string str = $"InstanceID: {InstanceID}\n";
        str += $"DefinitionID: {DefinitionID}\n";
        str += $"X: {X}, Y: {Y}, Rotation: {Rotation}\n";
        if (FlagOverrides != null) {
            str += $"FlagOverrides (object):\n{FlagOverrides.Stringify()}\n";
        }
        return str;
    }
}

public class InventoryItemInstance {
    public string InstanceID { get; set; }
    public string DefinitionID { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public InventoryItemRotation Rotation { get; set; }
    public float RotationRadians {
        get {
            return Mathf.Pi / 2 * (int)Rotation;
        }
    }
    public InventoryItemFlags? FlagOverrides { get; set; }

    public InventoryItemInstance(InventoryItemInstanceDTO dto) {
        if (!dto.IsValid()) {
            throw new ArgumentException("Invalid InventoryItemInstanceDTO");
        }
        if (string.IsNullOrEmpty(dto.InstanceID)) {
            InstanceID = AssetIDUtil.GenerateInventoryItemInstanceID();
        }
        else {
            InstanceID = dto.InstanceID;
        }
        DefinitionID = dto.DefinitionID!;
        X = dto.X;
        Y = dto.Y;
        Rotation = dto.Rotation;
        if (dto.FlagOverrides != null) {
            FlagOverrides = new InventoryItemFlags(dto.FlagOverrides);
        }
    }

    public void RotateClockwise() {
        Rotation = (InventoryItemRotation)(((int)Rotation + 1) % 4);
    }

    public void RotateCounterClockwise() {
        Rotation = (InventoryItemRotation)(((int)Rotation + 3) % 4);
    }
}

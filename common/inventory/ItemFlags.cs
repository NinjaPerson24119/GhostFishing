using System;

public class InventoryItemFlagsDTO : IGameAssetDTO {
    public bool CanPutInStorage { get; set; } = true;
    public bool CanBeMovedByPlayer { get; set; } = true;
    public bool CanDelete { get; set; } = true;
    public bool RestrictPlayerTeleport { get; set; } = false;

    public bool Validate() {
        return true;
    }

    public string Stringify() {
        string str = $"CanPutInStorage: {CanPutInStorage}\n";
        str += $"CanBeMovedByPlayer: {CanBeMovedByPlayer}\n";
        str += $"CanDelete: {CanDelete}\n";
        str += $"RestrictPlayerTeleport: {RestrictPlayerTeleport}";
        return str;
    }
}

public class InventoryItemFlags {
    public bool CanPutInStorage { get; set; } = true;
    public bool CanBeMovedByPlayer { get; set; } = true;
    public bool CanDelete { get; set; } = true;
    public bool RestrictPlayerTeleport { get; set; } = false;

    public InventoryItemFlags(InventoryItemFlagsDTO dto) {
        if (!dto.Validate()) {
            throw new ArgumentException("Invalid InventoryItemFlagsDTO");
        }
        CanPutInStorage = dto.CanPutInStorage;
        CanBeMovedByPlayer = dto.CanBeMovedByPlayer;
        CanDelete = dto.CanDelete;
        RestrictPlayerTeleport = dto.RestrictPlayerTeleport;
    }
}
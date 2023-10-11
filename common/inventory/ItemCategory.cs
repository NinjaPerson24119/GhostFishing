using System;
using Godot;

public class InventoryItemCategoryDTO : IGameAssetDTO, IGameAssetDTOWithImages {
    public string? Name { get; set; }
    public string? IconImagePath { get; set; }
    public string? BackgroundColor { get; set; }

    public bool IsValid() {
        if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(IconImagePath) || string.IsNullOrEmpty(BackgroundColor)) {
            return false;
        }
        return true;
    }

    public string Stringify() {
        return $"Name: {Name}\nIconImagePath: {IconImagePath}\nBackgroundColor: {BackgroundColor}";
    }

    public string[] ImageAssetPaths() {
        if (string.IsNullOrEmpty(IconImagePath)) {
            return new string[] { };
        }
        return new string[] { IconImagePath };
    }
}

public class InventoryItemCategory {
    public string Name { get; set; }
    public string IconImagePath { get; set; }
    public Color BackgroundColor { get; set; }

    public InventoryItemCategory(InventoryItemCategoryDTO dto) {
        if (!dto.IsValid()) {
            throw new ArgumentException("Invalid InventoryItemCategoryDTO");
        }
        Name = dto.Name!;
        IconImagePath = dto.IconImagePath!;
        BackgroundColor = new Color(dto.BackgroundColor!);
    }
}

using System;
using Godot;

public class InventoryItemDefinitionDTO : IGameAssetDTO {
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int CurrencyValue { get; set; }
    public string? ImagePath { get; set; }
    public string? SilhouetteImagePath { get; set; }
    public InventoryItemSpacePropertiesDTO? Space { get; set; }
    public InventoryItemFlagsDTO? Flags { get; set; }
    public string? CategoryID { get; set; }
    public string? BackgroundColorOverride { get; set; }

    public virtual bool IsValid() {
        if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Description)) {
            return false;
        }
        if (CurrencyValue < 0) {
            return false;
        }
        if (string.IsNullOrEmpty(ImagePath)) {
            return false;
        }
        // Silhouette is optional
        if (Space == null || !Space.IsValid()) {
            return false;
        }
        if (Flags == null || !Flags.IsValid()) {
            return false;
        }
        if (string.IsNullOrEmpty(CategoryID)) {
            return false;
        }
        // BackgroundColorOverride is optional
        return true;
    }

    public virtual string Stringify() {
        string str = $"Name: {Name}\nDescription: {Description}\n";
        str += $"CurrencyValue: {CurrencyValue}\n";
        str += $"ImagePath: {ImagePath}\n";
        if (SilhouetteImagePath != null) {
            str += $"SilhouetteImagePath: {SilhouetteImagePath}\n";
        }
        if (Space != null) {
            str += $"Space (object):\n{Space.Stringify()}\n";
        }
        if (Flags != null) {
            str += $"Flags (object):\n{Flags.Stringify()}\n";
        }
        str += $"Category: {CategoryID}";
        str += $"BackgroundColorOverride: {BackgroundColorOverride}";
        return str;
    }
}

public class InventoryItemDefinition {
    public string Name { get; set; }
    public string Description { get; set; }
    public int CurrencyValue { get; set; }
    public string ImagePath { get; set; }
    public string? SilhouetteImagePath { get; set; }
    public InventoryItemSpaceProperties Space { get; set; }
    public InventoryItemFlags Flags { get; set; }
    public string CategoryID { get; set; }
    public Color? BackgroundColorOverride { get; set; }

    public InventoryItemDefinition(InventoryItemDefinitionDTO dto) {
        if (!dto.IsValid()) {
            throw new ArgumentException("Invalid InventoryItemDefinitionDTO");
        }
        Name = dto.Name!;
        Description = dto.Description!;
        CurrencyValue = dto.CurrencyValue;
        ImagePath = dto.ImagePath!;
        SilhouetteImagePath = dto.SilhouetteImagePath;
        Space = new InventoryItemSpaceProperties(dto.Space!);
        Flags = new InventoryItemFlags(dto.Flags!);
        CategoryID = dto.CategoryID!;
        if (dto.BackgroundColorOverride == null) {
            BackgroundColorOverride = null;
        }
        else {
            BackgroundColorOverride = new Color(dto.BackgroundColorOverride);
        }
    }
}


using System;

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

    public virtual bool Validate() {
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
        if (Space == null || !Space.Validate()) {
            return false;
        }
        if (Flags == null || !Flags.Validate()) {
            return false;
        }
        if (string.IsNullOrEmpty(CategoryID)) {
            return false;
        }
        // BackgroundColorOverride is optional
        return true;
    }

    public virtual string Stringify() {
        string str = $"Name: {Name}\nDescription: {Description}";
        str += $"\nCurrencyValue: {CurrencyValue}\n";
        str += $"ImagePath: {ImagePath}\n";
        str += $"SilhouetteImagePath: {SilhouetteImagePath}\n";
        if (Space != null) {
            str += $"\nSpace:\n{Space.Stringify()}";
        }
        if (Flags != null) {
            str += $"\nFlags:\n{Flags.Stringify()}";
        }
        str += $"\nCategory:\n{CategoryID}";
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
    public string? BackgroundColorOverride { get; set; }

    public InventoryItemDefinition(InventoryItemDefinitionDTO dto) {
        if (!dto.Validate()) {
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
        BackgroundColorOverride = dto.BackgroundColorOverride;
    }
}


using System;
using Godot;
using System.Collections.Generic;

public class InventoryItemDefinitionDTO : IGameAssetDTO, IGameAssetDTOWithImages {
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
            GD.PrintErr($"Name or Description is null or empty.");
            return false;
        }
        if (CurrencyValue < 0) {
            GD.PrintErr($"CurrencyValue is less than 0.");
            return false;
        }
        if (string.IsNullOrEmpty(ImagePath)) {
            GD.PrintErr($"ImagePath is null or empty.");
            return false;
        }
        if (Space == null || !Space.IsValid()) {
            GD.Print($"Space is null or invalid.");
            return false;
        }
        if (Flags == null || !Flags.IsValid()) {
            GD.Print($"Flags is null or invalid.");
            return false;
        }
        if (!string.IsNullOrEmpty(CategoryID) && !AssetIDUtil.IsInventoryItemCategoryID(CategoryID)) {
            GD.Print($"CategoryID is invalid.");
            return false;
        }
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
        if (CategoryID != null) {
            str += $"Category: {CategoryID}";
        }
        str += $"BackgroundColorOverride: {BackgroundColorOverride}";
        return str;
    }

    public string[] ImageAssetPaths() {
        List<string> paths = new List<string>();
        if (!string.IsNullOrEmpty(ImagePath)) {
            paths.Add(ImagePath);
        }
        if (!string.IsNullOrEmpty(SilhouetteImagePath)) {
            paths.Add(SilhouetteImagePath);
        }
        return paths.ToArray();
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
    public string? CategoryID { get; set; }
    public Color? BackgroundColorOverride { get; set; }

    public InventoryItemDefinition(InventoryItemDefinitionDTO dto) {
        if (!dto.IsValid()) {
            throw new ArgumentException($"Invalid InventoryItemDefinitionDTO \n{dto.Stringify()}");
        }
        Name = dto.Name!;
        Description = dto.Description!;
        CurrencyValue = dto.CurrencyValue;
        ImagePath = dto.ImagePath!;
        SilhouetteImagePath = dto.SilhouetteImagePath;
        Space = new InventoryItemSpaceProperties(dto.Space!);
        Flags = new InventoryItemFlags(dto.Flags!);
        CategoryID = dto.CategoryID;
        if (dto.BackgroundColorOverride == null) {
            BackgroundColorOverride = null;
        }
        else {
            BackgroundColorOverride = new Color(dto.BackgroundColorOverride);
        }
    }
}


public class InventoryItemFlagsDTO {
    public bool CanPutInStorage { get; set; } = true;
    public bool CanBeMovedByPlayer { get; set; } = true;
    public bool CanDelete { get; set; } = true;
    public bool RestrictPlayerTeleport { get; set; } = false;
}

public class InventoryItemFlags {
    public bool CanPutInStorage { get; set; } = true;
    public bool CanBeMovedByPlayer { get; set; } = true;
    public bool CanDelete { get; set; } = true;
    public bool RestrictPlayerTeleport { get; set; } = false;

    public string Stringify() {
        string str = $"CanPutInStorage: {CanPutInStorage}\n";
        str += $"CanBeMovedByPlayer: {CanBeMovedByPlayer}\n";
        str += $"CanDelete: {CanDelete}\n";
        str += $"RestrictPlayerTeleport: {RestrictPlayerTeleport}";
        return str;
    }
}

public class InventoryItemCategoryDTO {
    public string Name { get; set; }
    public string IconImagePath { get; set; }
    public string BackgroundColor { get; set; }
}

public class InventoryItemCategory : IValidatedGameAsset {
    public string Name { get; set; }
    public string IconImagePath { get; set; }
    public string BackgroundColor { get; set; }

    public bool Validate() {
        return Name.Length > 0 && IconImagePath.Length > 0 && BackgroundColor.Length > 0;
    }

    public string Stringify() {
        return $"Name: {Name}\nIconImagePath: {IconImagePath}\nBackgroundColor: {BackgroundColor}";
    }
}

public class InventoryItemDefinitionDTO {
    public string Name { get; set; }
    public string Description { get; set; }
    public int CurrencyValue { get; set; }
    public string ImagePath { get; set; }
    public string SilhouetteImagePath { get; set; }
    public InventoryItemSpacePropertiesDTO Space { get; set; }
    public InventoryItemFlagsDTO Flags { get; set; }
    public InventoryItemCategoryDTO Category { get; set; }
    public string BackgroundColorOverride { get; set; }
}

public class InventoryItemDefinition : IValidatedGameAsset {
    public string Name { get; set; }
    public string Description { get; set; }
    public int CurrencyValue { get; set; }
    public string ImagePath { get; set; }
    public string SilhouetteImagePath { get; set; }
    public InventoryItemSpaceProperties Space { get; set; }
    public InventoryItemFlags Flags { get; set; }
    public InventoryItemCategory Category { get; set; }
    public string BackgroundColorOverride { get; set; }

    public virtual bool Validate() {
        if (Name.Length == 0 || Description.Length == 0) {
            return false;
        }
        if (CurrencyValue < 0) {
            return false;
        }
        if (ImagePath.Length == 0) {
            return false;
        }
        // Silhouette is optional
        if (Space == null || !Space.Validate()) {
            return false;
        }
        // Flags are optional
        if (Category == null || !Category.Validate()) {
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
        if (Category != null) {
            str += $"\nCategory:\n{Category.Stringify()}";
        }
        str += $"BackgroundColorOverride: {BackgroundColorOverride}";
        return str;
    }
}


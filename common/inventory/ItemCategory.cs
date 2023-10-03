public class InventoryItemCategoryDTO : IGameAssetDTO {
    public string? Name { get; set; }
    public string? IconImagePath { get; set; }
    public string? BackgroundColor { get; set; }

    public bool Validate() {
        if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(IconImagePath) || string.IsNullOrEmpty(BackgroundColor)) {
            return false;
        }
        return true;
    }

    public string Stringify() {
        return $"Name: {Name}\nIconImagePath: {IconImagePath}\nBackgroundColor: {BackgroundColor}";
    }
}

public class InventoryItemCategory {
    public string Name { get; set; }
    public string IconImagePath { get; set; }
    public string BackgroundColor { get; set; }
}

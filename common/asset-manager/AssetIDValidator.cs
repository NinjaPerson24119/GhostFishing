public class AssetIDValidator {
    public static bool ValidateInventoryID(string id) {
        return id.StartsWith("INVENTORY-");
    }

    public static bool ValidateInventoryItemDefinitionID(string id) {
        return id.StartsWith("ITEM-");
    }

    public static bool ValidateInventoryItemCategoryID(string id) {
        return id.StartsWith("CATEGORY-");
    }

    public static bool ValidateInventoryItemInstanceID(string id) {
        return id.StartsWith("ITEM_INSTANCE-");
    }

    public static bool ValidateQuestID(string id) {
        return id.StartsWith("QUEST-");
    }
}

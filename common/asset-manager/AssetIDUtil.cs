using System;

public static class AssetIDUtil {
    public static bool IsInventoryID(string id) {
        return id.StartsWith("INVENTORY-");
    }

    public static bool IsInventoryItemDefinitionID(string id) {
        return id.StartsWith("ITEM-");
    }

    public static bool IsInventoryItemCategoryID(string id) {
        return id.StartsWith("CATEGORY-");
    }

    public static bool IsInventoryItemInstanceID(string id) {
        return id.StartsWith("ITEM_INSTANCE-");
    }

    public static string GenerateInventoryItemInstanceID() {
        return $"ITEM_INSTANCE-{Guid.NewGuid()}";
    }

    public static bool IsQuestID(string id) {
        return id.StartsWith("QUEST-");
    }
}

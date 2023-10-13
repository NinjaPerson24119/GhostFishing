using System;

public static class AssetIDUtil {
    // validate
    public static bool IsInventoryInstanceID(string id) {
        return id.StartsWith("INVENTORY_INSTANCE-");
    }

    public static bool IsInventoryDefinitionID(string id) {
        return id.StartsWith("INVENTORY_DEF-");
    }

    public static bool IsInventoryItemInstanceID(string id) {
        return id.StartsWith("ITEM_INSTANCE-");
    }

    public static bool IsInventoryItemDefinitionID(string id) {
        return id.StartsWith("ITEM_DEF-");
    }

    public static bool IsInventoryItemCategoryID(string id) {
        return id.StartsWith("CATEGORY-");
    }

    public static bool IsQuestID(string id) {
        return id.StartsWith("QUEST-");
    }

    // generate
    public static string GenerateInventoryInstanceID() {
        return $"INVENTORY_INSTANCE-{Guid.NewGuid()}";
    }

    public static string GenerateInventoryItemInstanceID() {
        return $"ITEM_INSTANCE-{Guid.NewGuid()}";
    }
}

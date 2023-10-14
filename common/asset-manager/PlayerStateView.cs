internal struct PlayerStateAssetIDs {
    public string BoatInventoryID { get; }
    public string QuestInventoryID { get; }
    public string StorageID { get; }

    public PlayerStateAssetIDs(string boatInventoryID, string questInventoryID, string storageID) {
        BoatInventoryID = boatInventoryID;
        QuestInventoryID = questInventoryID;
        StorageID = storageID;
    }
}

internal class PlayerStateView {
    public Inventory BoatInventory;
    public Inventory QuestInventory;
    public Inventory StorageInventory;

    public PlayerStateView(PlayerStateAssetIDs assetIDs) {
        BoatInventory = AssetManager.Ref().GetInventory(assetIDs.BoatInventoryID);
        QuestInventory = AssetManager.Ref().GetInventory(assetIDs.QuestInventoryID);
        StorageInventory = AssetManager.Ref().GetInventory(assetIDs.StorageID);
    }
}

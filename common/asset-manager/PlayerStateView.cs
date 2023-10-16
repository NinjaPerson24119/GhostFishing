internal struct PlayerStateAssetIDs {
    public string BoatInventoryInstanceID { get; }
    public string QuestInventoryInstanceID { get; }
    public string StorageInventoryInstanceID { get; }

    public PlayerStateAssetIDs(string boatInventoryInstanceID, string questInventoryInstanceID, string storageInventoryInstanceID) {
        BoatInventoryInstanceID = boatInventoryInstanceID;
        QuestInventoryInstanceID = questInventoryInstanceID;
        StorageInventoryInstanceID = storageInventoryInstanceID;
    }
}

internal class PlayerStateView {
    public InventoryInstance BoatInventory;
    public InventoryInstance QuestInventory;
    public InventoryInstance StorageInventory;

    public PlayerStateView(PlayerStateAssetIDs assetIDs) {
        BoatInventory = AssetManager.Ref().GetInventoryInstance(assetIDs.BoatInventoryInstanceID);
        QuestInventory = AssetManager.Ref().GetInventoryInstance(assetIDs.QuestInventoryInstanceID);
        StorageInventory = AssetManager.Ref().GetInventoryInstance(assetIDs.StorageInventoryInstanceID);
    }
}

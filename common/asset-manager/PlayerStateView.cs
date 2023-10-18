using Godot;

public struct PlayerStateAssetIDs {
    public string BoatInventoryInstanceID { get; }
    public string QuestInventoryInstanceID { get; }
    public string StorageInventoryInstanceID { get; }

    public PlayerStateAssetIDs(string boatInventoryInstanceID, string questInventoryInstanceID, string storageInventoryInstanceID) {
        BoatInventoryInstanceID = boatInventoryInstanceID;
        QuestInventoryInstanceID = questInventoryInstanceID;
        StorageInventoryInstanceID = storageInventoryInstanceID;
    }
}

public class PlayerStateView {
    public InventoryInstance BoatInventory;
    public InventoryInstance QuestInventory;
    public InventoryInstance StorageInventory;

    public Vector3 GlobalPosition {
        get => _player.GlobalPosition;
    }

    private Player _player;

    public PlayerStateView(PlayerStateAssetIDs assetIDs, Player player) {
        BoatInventory = AssetManager.Ref().GetInventoryInstance(assetIDs.BoatInventoryInstanceID);
        QuestInventory = AssetManager.Ref().GetInventoryInstance(assetIDs.QuestInventoryInstanceID);
        StorageInventory = AssetManager.Ref().GetInventoryInstance(assetIDs.StorageInventoryInstanceID);

        _player = player;
    }
}

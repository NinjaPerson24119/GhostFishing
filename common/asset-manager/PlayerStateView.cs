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

    public readonly PlayerID PlayerID;
    public Vector3 GlobalPosition {
        get {
            var players = PlayerInjector.Ref().GetPlayers();
            if (!players.ContainsKey(PlayerID)) {
                throw new System.Exception($"Player {PlayerID} not found");
            }
            return players[PlayerID].GlobalPosition;
        }
    }
    public Vector3 GlobalRotation {
        get {
            var players = PlayerInjector.Ref().GetPlayers();
            if (!players.ContainsKey(PlayerID)) {
                throw new System.Exception($"Player {PlayerID} not found");
            }
            return players[PlayerID].GlobalRotation;
        }
    }

    public PlayerStateView(PlayerID playerID, PlayerStateAssetIDs assetIDs) {
        PlayerID = playerID;
        BoatInventory = AssetManager.Ref().GetInventoryInstance(assetIDs.BoatInventoryInstanceID);
        QuestInventory = AssetManager.Ref().GetInventoryInstance(assetIDs.QuestInventoryInstanceID);
        StorageInventory = AssetManager.Ref().GetInventoryInstance(assetIDs.StorageInventoryInstanceID);
    }
}

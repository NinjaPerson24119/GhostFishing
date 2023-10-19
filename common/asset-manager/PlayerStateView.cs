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
    public readonly PlayerID PlayerID;
    private readonly PlayerStateAssetIDs playerStateAssetIDs;

    public InventoryInstance BoatInventory {
        get => AssetManager.Ref().GetInventoryInstance(playerStateAssetIDs.BoatInventoryInstanceID);
    }
    public InventoryInstance QuestInventory {
        get => AssetManager.Ref().GetInventoryInstance(playerStateAssetIDs.QuestInventoryInstanceID);
    }
    public InventoryInstance StorageInventory {
        get => AssetManager.Ref().GetInventoryInstance(playerStateAssetIDs.StorageInventoryInstanceID);
    }

    public Vector3 GlobalPosition {
        get {
            var players = PlayerInjector.Ref().GetPlayers();
            if (!players.ContainsKey(PlayerID)) {
                throw new System.Exception($"Player {PlayerID} not found");
            }
            return players[PlayerID].GlobalPosition;
        }
        set {
            var players = PlayerInjector.Ref().GetPlayers();
            if (!players.ContainsKey(PlayerID)) {
                throw new System.Exception($"Player {PlayerID} not found");
            }
            players[PlayerID].GlobalPosition = value;
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
        set {
            var players = PlayerInjector.Ref().GetPlayers();
            if (!players.ContainsKey(PlayerID)) {
                throw new System.Exception($"Player {PlayerID} not found");
            }
            players[PlayerID].GlobalRotation = value;
        }
    }

    public CameraStateDTO? CameraState {
        get {
            var players = PlayerInjector.Ref().GetPlayers();
            if (!players.ContainsKey(PlayerID)) {
                throw new System.Exception($"Player {PlayerID} not found");
            }
            PlayerContext? playerContext = players[PlayerID].PlayerContext;
            if (playerContext == null) {
                return null;
            }
            return playerContext.FollowCamera.CameraState.ToDTO();
        }
        set {
            if (value == null) {
                return;
            }
            var players = PlayerInjector.Ref().GetPlayers();
            if (!players.ContainsKey(PlayerID)) {
                throw new System.Exception($"Player {PlayerID} not found");
            }
            PlayerContext? playerContext = players[PlayerID].PlayerContext;
            if (playerContext == null) {
                return;
            }
            playerContext.FollowCamera.SetCameraState(value.Value);
        }
    }

    public PlayerStateView(PlayerID playerID, PlayerStateAssetIDs assetIDs) {
        PlayerID = playerID;
        playerStateAssetIDs = assetIDs;
    }
}

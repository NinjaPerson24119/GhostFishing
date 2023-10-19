using Godot;

internal class OpenInventoryAction : InteractiveObjectAction {
    private string _inventoryInstanceID;

    public OpenInventoryAction(string inventoryInstanceID, float maxDistance = 6f) {
        _inventoryInstanceID = inventoryInstanceID;
        _preconditions.Add(new DistancePrecondition(maxDistance));
    }

    public override bool Activate(InteractiveObject interactiveObject, Player player) {
        bool baseResult = base.Activate(interactiveObject, player);
        if (!baseResult) {
            return false;
        }

        PlayerContext? playerContext = player.PlayerContext;
        if (playerContext == null) {
            throw new System.Exception("PlayerContext null");
        }
        GD.Print($"Activated inventory {_inventoryInstanceID}");
        InventoryInstance inventoryInstance = AssetManager.Ref().GetInventoryInstance(_inventoryInstanceID);
        return playerContext.OpenInventoryWithOthers(new InventoryInstance[] { inventoryInstance });
    }
}

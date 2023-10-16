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

        GD.Print($"Activated inventory {_inventoryInstanceID}");
        // TODO: call high-level API to open this inventory and player inventory side-by-side
        // also need to predicate by player
        return true;
    }
}

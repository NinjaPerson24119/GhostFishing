using Godot;

public class OpenInventoryAction : IInteractiveObjectAction {
    private string _inventoryInstanceID;

    public void Activate() {
        GD.Print($"Activated inventory {_inventoryInstanceID}");
        // TODO: call high-level API to open this inventory and player inventory side-by-side
        // also need to predicate by player
    }

    public string Description {
        get => _description;
    }
    private string _description;

    public OpenInventoryAction(string description, string inventoryInstanceID) {
        _description = description;
        _inventoryInstanceID = inventoryInstanceID;
    }
}

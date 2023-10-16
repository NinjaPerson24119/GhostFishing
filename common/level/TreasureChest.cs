using System;
using Godot;

public partial class TreasureChest : BuoyantBody {
    [Export]
    public string InventoryInstanceID = "";
    [Export]
    public string InteractiveObjectID = "";
    [Export]
    public string InteractiveObjectName = "";

    private InteractiveObject _interactiveObject;

    public TreasureChest() {
        if (string.IsNullOrEmpty(InventoryInstanceID)) {
            throw new Exception("InventoryInstanceID must be set");
        }
        if (string.IsNullOrEmpty(InteractiveObjectID)) {
            throw new Exception("InteractiveObjectID must be set");
        }
        if (string.IsNullOrEmpty(InteractiveObjectName)) {
            throw new Exception("InteractiveObjectName must be set");
        }

        OpenInventoryAction action = new OpenInventoryAction($"Open {InteractiveObjectName}", InventoryInstanceID);
        _interactiveObject = new InteractiveObject(InteractiveObjectID, this, action);
    }

    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);
        _interactiveObject.PhysicsProcess(delta);
    }
}

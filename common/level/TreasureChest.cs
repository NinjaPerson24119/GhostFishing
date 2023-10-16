using System;
using Godot;

public partial class TreasureChest : BuoyantBody {
    [Export]
    public string InventoryInstanceID = "";
    [Export]
    public string InteractiveObjectID = "";
    [Export]
    public string InteractiveObjectName = "";

    private InteractiveObject? _interactiveObject;

    public override void _Ready() {
        base._Ready();

        if (string.IsNullOrEmpty(InventoryInstanceID)) {
            throw new Exception("InventoryInstanceID must be set");
        }
        if (string.IsNullOrEmpty(InteractiveObjectID)) {
            throw new Exception("InteractiveObjectID must be set");
        }
        if (string.IsNullOrEmpty(InteractiveObjectName)) {
            throw new Exception("InteractiveObjectName must be set");
        }

        InteractiveObjectAction action = new OpenInventoryAction($"Open {InteractiveObjectName}", InventoryInstanceID);
        _interactiveObject = new InteractiveObject(InteractiveObjectID, this, action);
    }

    public override void _PhysicsProcess(double delta) {
        base._PhysicsProcess(delta);

        if (_interactiveObject == null) {
            throw new Exception("InteractiveObject is null");
        }
        _interactiveObject.PhysicsProcess(delta);
    }
}

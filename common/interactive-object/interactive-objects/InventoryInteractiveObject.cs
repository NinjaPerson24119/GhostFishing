using Godot;
using System;

public partial class InventoryInteractiveObject : InteractiveObject {
    [Export]
    public string InventoryName = "";
    [Export]
    public string InventoryInstanceID = "";
    [Export]
    public float MaxDistance = 6f;

    public override void _Ready() {
        base._Ready();

        if (string.IsNullOrEmpty(InventoryInstanceID)) {
            throw new Exception("InventoryInstanceID must be set");
        }
        if (string.IsNullOrEmpty(InventoryName)) {
            throw new Exception("InventoryName must be set");
        }

        Description = $"Open {InventoryName}";
        AddAction(new OpenInventoryAction(InventoryInstanceID, MaxDistance));
    }
}

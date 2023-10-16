using System;
using Godot;

public partial class TreasureChest : BuoyantBody {
    [Export]
    string? InventoryInstanceID;

    private InteractiveObject

    TreasureChest() : base() {
        if (InventoryInstanceID == null) {
            throw new Exception("InventoryInstanceID must be set");
        }

        var description = "Open treasure chest";
        Action = new OpenInventoryAction(description, InventoryInstanceID);
    }
}

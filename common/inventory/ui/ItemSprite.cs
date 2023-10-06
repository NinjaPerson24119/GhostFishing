using Godot;

public partial class InventoryItemSprite : Sprite2D {
    public string ItemInstanceID { get; private set; }

    public InventoryItemSprite(string itemInstanceID) {
        ItemInstanceID = itemInstanceID;
    }
}

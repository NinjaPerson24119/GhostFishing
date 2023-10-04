using Godot;

public partial class PlayerMenu : Menu {
    private InventoryFrame _inventoryFrame = null!;
    private InventoryItemTransport _itemTransport = new InventoryItemTransport();
    private Inventory _boatInventory = null!;

    public override void _Ready() {
        base._Ready();
        _closeActions.Add("open_inventory");

        PlayerStateView player = AssetManager.Ref().GetPlayerView(0);
        _boatInventory = player.BoatInventory;
        InventoryFrame boatInventoryFrame = new InventoryFrame();
        boatInventoryFrame.SetInventory(_boatInventory);
        AddChild(boatInventoryFrame);
    }

    public override void Open() {
        base.Open();
        CallDeferred(nameof(OpenInventory));
    }

    public void OpenInventory() {
        _itemTransport.OpenInventory(_boatInventory);
        GD.Print("Opened inventory.");
    }

    public override void Close() {
        _itemTransport.CloseInventory();
        base.Close();
        GD.Print("Closed inventory.");
    }
}

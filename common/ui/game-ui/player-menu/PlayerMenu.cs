using Godot;

public partial class PlayerMenu : Menu {
    private InventoryFrame _inventoryFrame = null!;
    private InventoryItemTransport _itemTransport = new InventoryItemTransport();
    private Inventory _boatInventory = null!;
    private InventoryFrame _boatInventoryFrame = null!;

    public override void _Ready() {
        base._Ready();
        _closeActions.Add("open_inventory");

        PlayerStateView player = AssetManager.Ref().GetPlayerView(0);
        _boatInventory = player.BoatInventory;
        _boatInventoryFrame = new InventoryFrame(_boatInventory);
        _boatInventoryFrame.SetAnchorsPreset(LayoutPreset.TopRight);

        Control inventoryContainer = new Control();
        // TODO: figure out how to align to top-right, why is this rocket science?
        inventoryContainer.SetAnchor(Side.Left, 0.75f);
        inventoryContainer.AddChild(_boatInventoryFrame);

        AddChild(inventoryContainer);
    }

    public override void Open() {
        base.Open();
        CallDeferred(nameof(OpenInventory));
    }

    public void OpenInventory() {
        _itemTransport.OpenInventory(_boatInventory);
        _boatInventoryFrame.Focus();
        GD.Print("Opened inventory.");
    }

    public override void Close() {
        _itemTransport.CloseInventory();
        base.Close();
        GD.Print("Closed inventory.");
    }
}

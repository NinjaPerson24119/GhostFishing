using Godot;

public partial class PlayerMenu : Menu {
    [Export]
    public int TileSizePx = 64;

    private InventoryFrame _inventoryFrame = null!;
    private InventoryItemTransport _itemTransport = null!;
    private Inventory _boatInventory = null!;
    private InventoryFrame _boatInventoryFrame = null!;

    public override void _Ready() {
        base._Ready();
        _closeActions.Add("open_inventory");

        PlayerStateView player = AssetManager.Ref().GetPlayerView(0);
        _boatInventory = player.BoatInventory;
        _boatInventoryFrame = new InventoryFrame(_boatInventory, TileSizePx) {
            Name = "BoatInventoryFrame"
        };
        _boatInventoryFrame.SetAnchorsPreset(LayoutPreset.TopRight);

        Control inventoryContainer = new Control() {
            Name = "InventoryLayout"
        };
        // TODO: figure out how to align to top-right, why is this rocket science?
        inventoryContainer.SetAnchor(Side.Left, 0.75f);
        inventoryContainer.AddChild(_boatInventoryFrame);
        AddChild(inventoryContainer);

        _itemTransport = new InventoryItemTransport(TileSizePx);
        AddChild(_itemTransport);
    }

    public override void _Input(InputEvent inputEvent) {
        base._Input(inputEvent);
        if (!AcceptingInput) {
            return;
        }

        if (inputEvent.IsActionPressed("select")) {
            if (!_itemTransport.HasItem()) {
                GD.Print("Taking item.");
                _itemTransport.TakeItem();
            }
            else {
                GD.Print("Placing item.");
                _itemTransport.PlaceItem();
            }
        }
        if (inputEvent.IsActionPressed("cancel")) {
            _itemTransport.RevertTakeItem();
        }
        if (inputEvent.IsActionPressed("inventory_rotate_clockwise")) {
            _itemTransport.RotateClockwise();
        }
        if (inputEvent.IsActionPressed("inventory_rotate_counterclockwise")) {
            _itemTransport.RotateCounterClockwise();
        }

        CloseActionClosesMenu = _itemTransport.HasItem();
    }

    public override void Open() {
        base.Open();
        CallDeferred(nameof(OpenInventory));
    }

    public void OpenInventory() {
        _itemTransport.OpenInventory(_boatInventory, _boatInventoryFrame);
        GD.Print("Opened inventory.");
    }

    public override void Close() {
        _itemTransport.CloseInventory();
        base.Close();
        GD.Print("Closed inventory.");
    }
}

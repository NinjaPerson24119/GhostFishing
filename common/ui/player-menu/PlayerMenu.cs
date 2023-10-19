using Godot;
using System;

public partial class PlayerMenu : Menu {
    [Export]
    public int TileSizePx = 64;

    private InventoryItemTransport? _itemTransport;
    private InventoryInstance? _boatInventory;
    private InventoryFrame? _boatInventoryFrame;
    private bool _initialized = false;
    private SaveStateManager.Lock? _saveStateLock;
    private PlayerContext? _playerContext;

    public PlayerMenu() {
        Visible = false;
    }

    public override void _Ready() {
        base._Ready();

        _playerContext = DependencyInjector.Ref().GetLocalPlayerContext(GetPath());
        if (_playerContext == null) {
            throw new Exception("PlayerContext null");
        }
        _closeActions.Add(_playerContext.ActionCancel);
        _closeActions.Add(_playerContext.ActionOpenInventory);
        SaveStateManager.Ref().LoadedSaveState += OnLoadedSaveState;

        Initialize();
    }

    public override void _ExitTree() {
        Close();
        SaveStateManager.Ref().LoadedSaveState -= OnLoadedSaveState;
    }

    public void Initialize() {
        if (_initialized) {
            throw new Exception("PlayerMenu already initialized");
        }
        if (_playerContext == null) {
            throw new Exception("PlayerContext null");
        }
        PlayerStateView playerView = AssetManager.Ref().GetPlayerView(_playerContext.PlayerID);
        _boatInventory = playerView.BoatInventory;
        _boatInventoryFrame = new InventoryFrame(_boatInventory, TileSizePx);
        _boatInventoryFrame.SetAnchorsPreset(LayoutPreset.TopRight);
        _boatInventoryFrame.Resized += OnInventoryFrameResized;
        AddChild(_boatInventoryFrame);

        _itemTransport = new InventoryItemTransport(TileSizePx);
        AddChild(_itemTransport);

        _initialized = true;
    }

    public override void _Input(InputEvent inputEvent) {
        base._Input(inputEvent);

        if (!_initialized) {
            throw new Exception("PlayerMenu not initialized");
        }
        if (_itemTransport == null) {
            return;
        }
        if (!AcceptingInput) {
            return;
        }
        if (_playerContext == null) {
            throw new Exception("PlayerContext must be set before _Input is called");
        }

        if (inputEvent.IsActionPressed(_playerContext.ActionSelect)) {
            if (!_itemTransport.HasItem()) {
                _itemTransport.TakeItem();
            }
            else {
                _itemTransport.PlaceItem();
            }
        }
        if (inputEvent.IsActionPressed(_playerContext.ActionCancel)) {
            _itemTransport.RevertTakeItem();
        }
        if (inputEvent.IsActionPressed(_playerContext.ActionRotateClockwise)) {
            _itemTransport.RotateClockwise();
        }
        if (inputEvent.IsActionPressed(_playerContext.ActionRotateCounterClockwise)) {
            _itemTransport.RotateCounterClockwise();
        }

        CloseActionClosesMenu = !_itemTransport.HasItem();
        if (_itemTransport.HasItem() && _saveStateLock == null) {
            _saveStateLock = SaveStateManager.Ref().GetLock();
        }
        else if (!_itemTransport.HasItem() && _saveStateLock != null) {
            _saveStateLock.Dispose();
            _saveStateLock = null;
        }
    }

    public override void Open() {
        base.Open();
        CallDeferred(nameof(OpenInventory));
    }

    public void OpenInventory() {
        if (_itemTransport == null || _boatInventory == null || _boatInventoryFrame == null || !_initialized) {
            throw new Exception("Cannot OpenInventory because PlayerMenu not initialized");
        }
        if (!_itemTransport.IsOpen()) {
            _itemTransport.OpenInventory(_boatInventory, _boatInventoryFrame);
            GD.Print("Opened inventory.");
        }
        else {
            GD.PrintErr("Cannot open item transport because it's already open.");
        }
    }

    public override void Close() {
        if (_itemTransport != null) {
            _itemTransport.CloseInventory();
            GD.Print("Closed inventory.");
        }
        base.Close();
    }

    private void OnInventoryFrameResized() {
        if (_boatInventoryFrame == null) {
            return;
        }
        _boatInventoryFrame.OffsetLeft = -_boatInventoryFrame.Size.X;
    }

    public void OnLoadedSaveState() {
        if (_initialized == false) {
            return;
        }
        GD.Print("PlayerMenu.OnLoadedSaveState");

        _initialized = false;
        if (_itemTransport != null) {
            _itemTransport.CloseInventory();
            _itemTransport.QueueFree();
            _itemTransport = null;
        }
        _boatInventory = null;
        if (_boatInventoryFrame != null) {
            _boatInventoryFrame.QueueFree();
            _boatInventoryFrame = null;
        }
        if (_saveStateLock != null) {
            _saveStateLock.Dispose();
            _saveStateLock = null;
        }
        Initialize();
        CallDeferred(nameof(OpenInventory));
    }
}

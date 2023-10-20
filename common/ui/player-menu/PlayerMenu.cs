using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerMenu : Menu {
    [Export]
    public int TileSizePx = 64;

    private InventoryItemTransport? _itemTransport;
    private InventoryInstance? _boatInventory;
    private InventoryFrame? _boatInventoryFrame;
    private List<InventoryFrame> _additionalInventoryFrames = new List<InventoryFrame>();
    private bool _initialized = false;
    private SaveStateManager.Lock? _saveStateLock;
    private PlayerContext? _playerContext;
    private Container? _framesContainer;

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

        _framesContainer = GetNode<VBoxContainer>("VBoxContainer");

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
        if (_framesContainer == null) {
            throw new Exception("InventoryFramesContainer null");
        }
        PlayerStateView playerView = AssetManager.Ref().GetPlayerView(_playerContext.PlayerID);
        _boatInventory = playerView.BoatInventory;
        _boatInventoryFrame = StyledInventoryFrame(new InventoryFrame(_boatInventory, TileSizePx));
        _framesContainer.AddChild(_boatInventoryFrame);

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
        if (inputEvent.IsActionPressed(_playerContext.ActionNextInventoryFrame)) {
            _itemTransport.SelectNextInventoryFrame();
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
        OpenInventories(null);
    }

    public void OpenInventories(InventoryInstance[]? additionalInventories) {
        if (_itemTransport == null || _boatInventory == null || _boatInventoryFrame == null || !_initialized) {
            throw new Exception("Cannot OpenInventory because PlayerMenu not initialized");
        }
        if (!_itemTransport.IsOpen(_boatInventory.InventoryInstanceID)) {
            _itemTransport.OpenInventory(_boatInventory, _boatInventoryFrame);
            _itemTransport.SelectInventoryFrame(_boatInventory.InventoryInstanceID);
            GD.Print("Opened inventory.");
        }
        if (_playerContext == null) {
            throw new Exception("PlayerContext null");
        }

        if (_framesContainer == null) {
            throw new Exception("InventoryFramesContainer null");
        }
        if (additionalInventories != null) {
            foreach (InventoryInstance inventory in additionalInventories) {
                InventoryFrame frame = StyledInventoryFrame(new InventoryFrame(inventory, TileSizePx));
                _framesContainer.AddChild(frame);
                _additionalInventoryFrames.Add(frame);

                _itemTransport.OpenInventory(inventory, frame);
                GD.Print("Opened additional inventory.");
            }
        }
    }

    public override void Close() {
        if (_itemTransport != null) {
            _itemTransport.CloseInventories();
            ClearAdditionalInventoryFrames();
            GD.Print("Closed inventories.");
        }
        base.Close();
    }

    public void OnLoadedSaveState() {
        if (_initialized == false) {
            return;
        }
        GD.Print("PlayerMenu.OnLoadedSaveState");

        _initialized = false;
        if (_itemTransport != null) {
            _itemTransport.CloseInventories();
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
        ClearAdditionalInventoryFrames();

        Initialize();
        CallDeferred(nameof(OpenInventory));
    }

    public bool OpenInventoryWithOthers(InventoryInstance[] inventories) {
        GD.Print("Would open other inventories");
        base.Open();
        OpenInventories(inventories);
        return true;
    }

    public void ClearAdditionalInventoryFrames() {
        foreach (InventoryFrame frame in _additionalInventoryFrames) {
            frame.QueueFree();
        }
        _additionalInventoryFrames.Clear();
    }

    private static InventoryFrame StyledInventoryFrame(InventoryFrame frame) {
        frame.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        return frame;
    }
}

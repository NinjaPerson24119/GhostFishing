using Godot;
using System;

public enum ControlsContextType {
    // player can open menus
    Player = 0,
    // player is in a menu so cannot be controlled
    PlayerMenu = 1,
}

public partial class PlayerController : Node {
    public ControlsContextType ControlsContext {
        get => _controlsContext;
        private set {
            _controlsContext = value;
            EmitSignal(SignalName.PlayerControlsContextChanged, (int)_controlsContext);
            EmitSignal(SignalName.SetPlayerControlsDisabled, value != ControlsContextType.Player);
        }
    }
    private ControlsContextType _controlsContext = ControlsContextType.Player;

    private PlayerContext? _playerContext;

    [Signal]
    public delegate void SetPlayerControlsDisabledEventHandler(bool disabled);
    [Signal]
    public delegate void PlayerControlsContextChangedEventHandler(ControlsContextType controlsContext);
    [Signal]
    public delegate void InputTypeChangedEventHandler(InputType inputType);


    public override void _Ready() {
        _playerContext = DependencyInjector.Ref().GetLocalPlayerContext(GetPath());
        EmitSignal(SignalName.PlayerControlsContextChanged, (int)_controlsContext);

        InputTypeController.Ref().InputTypeChanged += OnInputTypeChanged;
        if (_playerContext == null) {
            throw new Exception("PlayerContext null");
        }
        _playerContext.PlayerMenu.Opened += OnPlayerMenuOpened;
    }

    public override void _ExitTree() {
        InputTypeController.Ref().InputTypeChanged -= OnInputTypeChanged;
    }

    public override void _Process(double delta) {
        if (_playerContext == null) {
            return;
        }
        if (Input.IsActionJustPressed(_playerContext.ActionOpenInventory)) {
            _playerContext.PlayerMenu.Open();
            GD.Print("Player menu opened");
        }
        else {
            ProcessPlayerMenu();
        }
    }

    public void ProcessPlayerMenu() {
        if (_playerContext == null) {
            throw new Exception("PlayerContext must be set before _Process is called");
        }
        if (ControlsContext != ControlsContextType.PlayerMenu) {
            return;
        }
        Menu menu = _playerContext.PlayerMenu;
        if (menu.IsOpen && menu.RequestedClose) {
            menu.Close();
            ControlsContext = ControlsContextType.Player;
        }
    }

    public bool MouseAllowed() {
        // for now, the input mode is independent of the player
        return InputTypeController.Ref().MouseAllowed();
    }

    public InputType InputType {
        // for now, the input mode is independent of the player
        get => InputTypeController.Ref().InputType;
    }

    public void OnInputTypeChanged(InputType inputType) {
        // Forward the signal to remove tight coupling
        EmitSignal(SignalName.InputTypeChanged, (int)inputType);
    }

    public void OnPlayerMenuOpened() {
        ControlsContext = ControlsContextType.PlayerMenu;
    }
}

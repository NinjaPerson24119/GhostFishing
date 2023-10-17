using Godot;
using System;

public enum ControllerInputType {
    KeyboardMouse = 0,
    Joypad = 1,
}

public partial class PlayerController : Node {
    private enum ControlsContextType {
        // player can open menus
        Player = 0,
        // player is in a menu so cannot be controlled
        PlayerMenu = 1,
    }

    private ControlsContextType ControlsContext {
        get => _controlsContext;
        set {
            _controlsContext = value;
            EmitSignal(SignalName.SetPlayerControlsDisabled, value != ControlsContextType.Player);
        }
    }
    private ControlsContextType _controlsContext = ControlsContextType.Player;

    public ControllerInputType InputType {
        get => _inputType;
        private set {
            if (value != _inputType) {
                if (value != ControllerInputType.KeyboardMouse || MouseAllowed()) {
                    EmitSignal(SignalName.InputTypeChanged, (int)value);
                }
            }
            OnInputTypeChanged(value);
            _inputType = value;
        }
    }
    private ControllerInputType _inputType = ControllerInputType.KeyboardMouse;

    private PlayerContext? _playerContext;

    [Signal]
    public delegate void InputTypeChangedEventHandler(ControllerInputType inputType);
    [Signal]
    public delegate void SetPlayerControlsDisabledEventHandler(bool disabled);

    public override void _Ready() {
        _playerContext = DependencyInjector.Ref().GetLocalPlayerContext(GetPath());
    }

    public override void _Process(double delta) {
        ProcessPlayerMenu();
    }

    public override void _Input(InputEvent inputEvent) {
        if (inputEvent is InputEventJoypadButton || inputEvent is InputEventJoypadMotion) {
            InputType = ControllerInputType.Joypad;
        }
        else if (inputEvent is InputEventKey || inputEvent is InputEventMouse || inputEvent is InputEventMouseMotion || inputEvent is InputEventMouseButton) {
            InputType = ControllerInputType.KeyboardMouse;
        }

        TryOpenPlayerMenu(inputEvent);
    }

    public void TryOpenPlayerMenu(InputEvent inputEvent) {
        if (_playerContext == null) {
            throw new Exception("PlayerContext must be set before _Process is called");
        }
        if (ControlsContext != ControlsContextType.Player) {
            return;
        }
        if (inputEvent.IsActionPressed(_playerContext.ActionOpenInventory)) {
            ControlsContext = ControlsContextType.PlayerMenu;
            _playerContext.PlayerMenu.Open();
            return;
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

    public void OnInputTypeChanged(ControllerInputType inputType) {
        if (inputType == ControllerInputType.KeyboardMouse) {
            // TODO: set this to confined once I'm OK with the mouse getting locked constantly
            Input.MouseMode = ControlsContext == ControlsContextType.Player ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Visible;
        }
        else if (inputType == ControllerInputType.Joypad) {
            //Input.MouseMode = Input.MouseModeEnum.ConfinedHidden;
        }
    }

    public bool MouseAllowed() {
        if (_playerContext == null) {
            throw new Exception("PlayerContext null when checking if mouse is allowed");
        }
        return _playerContext.PlayerID == CoopManager.PlayerID.One && !CoopManager.Ref().CoopActive;
    }
}

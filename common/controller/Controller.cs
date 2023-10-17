using Godot;
using System;

public enum ControllerInputType {
    KeyboardMouse = 0,
    Joypad = 1,
}

internal partial class Controller : Node {
    private enum ControlsContextType {
        // player can open menus
        Player = 0,
        // player is in a menu so cannot be controlled
        PlayerMenu = 1,
        // pause menu open implies both player and player menus cannot be controlled
        PauseMenu = 2,
    }

    static SingletonTracker<Controller> _singletonTracker = new SingletonTracker<Controller>();
    private static Controller _singleton { get => _singletonTracker.Ref(); }

    private ControlsContextType ControlsContext {
        get => _controlsContext;
        set {
            if (value == ControlsContextType.PauseMenu) {
                PauseGame();
            }
            if (_controlsContext == ControlsContextType.PauseMenu && value != ControlsContextType.PauseMenu) {
                UnpauseGame();
            }
            _controlsContext = value;
            EmitSignal(SignalName.SetPlayerControlsDisabled, value != ControlsContextType.Player);
        }
    }
    private ControlsContextType _controlsContext = ControlsContextType.Player;

    public ControllerInputType InputType {
        get => _inputType;
        private set {
            if (value != _inputType) {
                EmitSignal(SignalName.InputTypeChanged, (int)value);
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
        _singletonTracker.Ready(this);
        _playerContext = DependencyInjector.Ref().GetPlayerOne().PlayerContext;
    }

    public override void _Process(double delta) {
        ProcessPlayerMenu();
        ProcessPauseMenu();
    }

    public override void _Input(InputEvent inputEvent) {
        if (inputEvent is InputEventJoypadButton || inputEvent is InputEventJoypadMotion) {
            InputType = ControllerInputType.Joypad;
        }
        else if (inputEvent is InputEventKey || inputEvent is InputEventMouse || inputEvent is InputEventMouseMotion || inputEvent is InputEventMouseButton) {
            InputType = ControllerInputType.KeyboardMouse;
        }

        TryOpenPlayerMenu(inputEvent);
        TryOpenPauseMenu(inputEvent);
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

    public void TryOpenPauseMenu(InputEvent inputEvent) {
        if (_playerContext == null) {
            throw new Exception("PlayerContext must be set before _Process is called");
        }
        if (ControlsContext != ControlsContextType.Player && ControlsContext != ControlsContextType.PlayerMenu) {
            return;
        }
        if (inputEvent.IsActionPressed("pause_menu")) {
            ControlsContext = ControlsContextType.PauseMenu;
            _playerContext.PlayerMenu.Disabled = true;
            DependencyInjector.Ref().GetPauseMenu().Open();
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

    public void ProcessPauseMenu() {
        if (_playerContext == null) {
            throw new Exception("PlayerContext must be set before _Process is called");
        }
        if (ControlsContext != ControlsContextType.PauseMenu && ControlsContext != ControlsContextType.PlayerMenu) {
            return;
        }
        Menu pauseMenu = DependencyInjector.Ref().GetPauseMenu();
        if (pauseMenu.IsOpen && pauseMenu.RequestedClose) {
            pauseMenu.Close();

            Menu playerMenu = _playerContext.PlayerMenu;
            playerMenu.Disabled = false;
            ControlsContext = playerMenu.IsOpen ? ControlsContextType.PlayerMenu : ControlsContextType.Player;
        }
    }

    public void PauseGame() {
        RealClock.Ref().Paused = true;
        GetTree().Paused = true;
    }

    public void UnpauseGame() {
        RealClock.Ref().Paused = false;
        GetTree().Paused = false;
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
}

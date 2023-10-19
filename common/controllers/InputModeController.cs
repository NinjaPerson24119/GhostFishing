using Godot;

public enum InputType {
    KeyboardMouse = 0,
    Joypad = 1,
}

public partial class InputModeController : Node {
    static SingletonTracker<InputModeController> _singletonTracker = new SingletonTracker<InputModeController>();
    private static InputModeController _singleton { get => _singletonTracker.Ref(); }
    public static InputModeController Ref() {
        return _singleton;
    }

    public InputType InputType {
        get => _inputType;
        private set {
            if (value == _inputType) {
                return;
            }
            if (value == InputType.KeyboardMouse && !MouseAllowed()) {
                return;
            }
            if (PlayerManager.Ref().CoopActive && value != InputType.Joypad) {
                return;
            }
            _inputType = value;
            UpdateMouseBehavior(_inputType);
            EmitSignal(SignalName.InputTypeChanged, (int)_inputType);
        }
    }
    private InputType _inputType = InputType.KeyboardMouse;

    private ControlsContextType _playerOneControlsContext = ControlsContextType.Player;

    // TODO: set these once ready to deal with captured mouse
    public const Input.MouseModeEnum SinglePlayerMouseModePlayer = Input.MouseModeEnum.Visible; //Input.MouseModeEnum.Captured;
    public const Input.MouseModeEnum SinglePlayerMouseModeMenu = Input.MouseModeEnum.Visible;
    public const Input.MouseModeEnum CoopMouseMode = Input.MouseModeEnum.Hidden; // Input.MouseModeEnum.ConfinedHidden

    [Signal]
    public delegate void InputTypeChangedEventHandler(InputType inputType);

    public InputModeController() {
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready() {
        _singletonTracker.Ready(this);

        PlayerManager.Ref().CoopChanged += OnCoopChanged;
        PlayerInjector.Ref().GetPlayerOneContext().Controller.PlayerControlsContextChanged += OnPlayerControlsContextChanged;
    }

    public override void _Input(InputEvent inputEvent) {
        if (inputEvent is InputEventJoypadButton || inputEvent is InputEventJoypadMotion) {
            InputType = InputType.Joypad;
        }
        else if (inputEvent is InputEventKey || inputEvent is InputEventMouse || inputEvent is InputEventMouseMotion || inputEvent is InputEventMouseButton) {
            InputType = InputType.KeyboardMouse;
        }
    }

    public bool MouseAllowed() {
        return !PlayerManager.Ref().CoopActive;
    }

    public void UpdateMouseBehavior(InputType inputType) {
        // this is a sort of abuse of handling the player / input controller boundary
        // but we really only care about the mouse behavior for single player
        if (!PlayerManager.Ref().CoopActive) {
            // need to check the input context if we're not in co-op mode
            if (inputType == InputType.KeyboardMouse) {
                Input.MouseMode = _playerOneControlsContext == ControlsContextType.Player ? SinglePlayerMouseModePlayer : SinglePlayerMouseModeMenu;
            }
            else if (inputType == InputType.Joypad) {
                Input.MouseMode = SinglePlayerMouseModeMenu;
            }
            else {
                GD.PrintErr($"Unknown input type {inputType}. ControlsContextType was probably updated without adjusting InputModeController");
            }
        }
        else {
            // in co-op mode, we always want the mouse hidden
            Input.MouseMode = CoopMouseMode;
        }
    }

    public void OnCoopChanged(bool coopActive) {
        UpdateMouseBehavior(InputType);
    }

    public void OnPlayerControlsContextChanged(ControlsContextType controlsContext) {
        _playerOneControlsContext = controlsContext;
        UpdateMouseBehavior(InputType);
    }
}

using Godot;

public partial class CommonController : Node {
    static SingletonTracker<CommonController> _singletonTracker = new SingletonTracker<CommonController>();
    private static CommonController _singleton { get => _singletonTracker.Ref(); }
    private bool _paused = false;

    [Signal]
    public delegate void SetPlayerControlsDisabledEventHandler(bool disabled);

    public override void _Ready() {
        _singletonTracker.Ready(this);

        CoopManager.Ref().CoopChanged += OnCoopChanged;
        Input.JoyConnectionChanged += OnJoyConnectionChanged;
    }

    public override void _Process(double delta) {
        ProcessPauseMenu();
    }

    public override void _Input(InputEvent inputEvent) {
        Menu pauseMenu = DependencyInjector.Ref().GetPauseMenu();
        if (!pauseMenu.IsOpen && inputEvent.IsActionPressed("pause_menu")) {
            OpenPauseMenu();
            pauseMenu.GrabFocus();
        }
    }

    public void ProcessPauseMenu() {
        Menu pauseMenu = DependencyInjector.Ref().GetPauseMenu();
        if (pauseMenu.IsOpen && pauseMenu.RequestedClose) {
            pauseMenu.Close();
            UnpauseGame();
            EmitSignal(SignalName.SetPlayerControlsDisabled, _paused);
        }
    }

    public void OpenPauseMenu() {
        Menu pauseMenu = DependencyInjector.Ref().GetPauseMenu();
        if (pauseMenu.IsOpen) {
            return;
        }

        PauseGame();
        pauseMenu.Open();
        EmitSignal(SignalName.SetPlayerControlsDisabled, _paused);
    }

    public void ClosePauseMenu() {
        Menu pauseMenu = DependencyInjector.Ref().GetPauseMenu();
        if (!pauseMenu.IsOpen) {
            return;
        }

        pauseMenu.Close();
        UnpauseGame();
        EmitSignal(SignalName.SetPlayerControlsDisabled, _paused);
    }

    public void PauseGame() {
        RealClock.Ref().Paused = true;
        GetTree().Paused = true;
    }

    public void UnpauseGame() {
        RealClock.Ref().Paused = false;
        GetTree().Paused = false;
    }

    public void OnCoopChanged(bool coopActive) {
        if (!coopActive) {
            OpenPauseMenu();
        }
    }

    public void OnJoyConnectionChanged(long device, bool connected) {
        if (!connected) {
            OpenPauseMenu();
        }
    }
}

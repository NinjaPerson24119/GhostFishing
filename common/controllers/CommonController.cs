using Godot;

public partial class CommonController : Node {
    static SingletonTracker<CommonController> _singletonTracker = new SingletonTracker<CommonController>();
    private static CommonController _singleton { get => _singletonTracker.Ref(); }

    [Signal]
    public delegate void SetPlayerControlsDisabledEventHandler(bool disabled);

    public override void _Ready() {
        _singletonTracker.Ready(this);

        PlayerManager.Ref().CoopChanged += OnCoopChanged;
        Input.JoyConnectionChanged += OnJoyConnectionChanged;
    }

    public override void _Process(double delta) {
        Menu pauseMenu = DependencyInjector.Ref().GetPauseMenu();
        if (!pauseMenu.IsOpen && Input.IsActionJustPressed("pause_menu")) {
            OpenPauseMenu();
            pauseMenu.GrabFocus();
        }
        else if (pauseMenu.IsOpen && pauseMenu.RequestedClose) {
            pauseMenu.Close();
            UnpauseGame();
            EmitSignal(SignalName.SetPlayerControlsDisabled, GetTree().Paused);
        }
    }

    public void OpenPauseMenu() {
        Menu pauseMenu = DependencyInjector.Ref().GetPauseMenu();
        if (pauseMenu.IsOpen) {
            return;
        }

        PauseGame();
        pauseMenu.Open();
        EmitSignal(SignalName.SetPlayerControlsDisabled, GetTree().Paused);
    }

    public void ClosePauseMenu() {
        Menu pauseMenu = DependencyInjector.Ref().GetPauseMenu();
        if (!pauseMenu.IsOpen) {
            return;
        }

        pauseMenu.Close();
        UnpauseGame();
        EmitSignal(SignalName.SetPlayerControlsDisabled, GetTree().Paused);
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

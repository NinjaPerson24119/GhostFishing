using Godot;

public partial class CommonController : Node {
    static SingletonTracker<CommonController> _singletonTracker = new SingletonTracker<CommonController>();
    private static CommonController _singleton { get => _singletonTracker.Ref(); }
    private bool _paused = false;

    [Signal]
    public delegate void SetPlayerControlsDisabledEventHandler(bool disabled);

    public override void _Ready() {
        _singletonTracker.Ready(this);
    }

    public override void _Process(double delta) {

    }

    public override void _Input(InputEvent inputEvent) {
        Menu pauseMenu = DependencyInjector.Ref().GetPauseMenu();
        if (!pauseMenu.IsOpen && inputEvent.IsActionPressed("pause_menu")) {
            PauseGame();
            pauseMenu.Open();
            EmitSignal(SignalName.SetPlayerControlsDisabled, _paused);
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

    public void PauseGame() {
        RealClock.Ref().Paused = true;
        GetTree().Paused = true;
    }

    public void UnpauseGame() {
        RealClock.Ref().Paused = false;
        GetTree().Paused = false;
    }
}

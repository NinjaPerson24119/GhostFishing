using Godot;

public partial class CommonController : Node {
    static SingletonTracker<CommonController> _singletonTracker = new SingletonTracker<CommonController>();
    private static CommonController _singleton { get => _singletonTracker.Ref(); }
    public static CommonController Ref() {
        return _singleton;
    }

    public CommonController() {
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready() {
        _singletonTracker.Ready(this);

        PlayerManager.Ref().CoopChanged += OnCoopChanged;
        Input.JoyConnectionChanged += OnJoyConnectionChanged;
    }

    public override void _Process(double delta) {
        Menu pauseMenu = DependencyInjector.Ref().GetPauseMenu();
        if (!pauseMenu.IsOpen && Input.IsActionJustPressed("pause_menu")) {
            OpenPauseMenu();
        }
        else if (pauseMenu.IsOpen && pauseMenu.RequestedClose) {
            pauseMenu.Close();
            UnpauseGame();
        }
    }

    public void OpenPauseMenu() {
        Menu pauseMenu = DependencyInjector.Ref().GetPauseMenu();
        if (pauseMenu.IsOpen) {
            return;
        }

        PauseGame();
        pauseMenu.Open();
    }

    public void ClosePauseMenu() {
        Menu pauseMenu = DependencyInjector.Ref().GetPauseMenu();
        if (!pauseMenu.IsOpen) {
            return;
        }

        pauseMenu.Close();
        UnpauseGame();
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
        if (!connected && InputTypeController.Ref().InputType == InputType.Joypad) {
            OpenPauseMenu();
        }
    }
}

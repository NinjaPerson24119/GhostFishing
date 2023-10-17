using Godot;

internal partial class PauseMenu : Menu {
    private Button? _resume;
    private Button? _coopPrompt;
    private Label? _controllerPrompt;

    public PauseMenu() {
        FocusMode = FocusModeEnum.All;
    }

    public override void _Ready() {
        base._Ready();

        _closeActions.Add("pause_menu");
        Player[] players = DependencyInjector.Ref().GetPlayers();
        foreach (Player player in players) {
            if (player.PlayerContext == null) {
                throw new System.Exception("PlayerContext must be set before _Ready is called");
            }
            _closeActions.Add(player.PlayerContext.ActionCancel);
        }

        _resume = GetNode<Button>("BoxContainer/Resume");
        _coopPrompt = GetNode<Button>("BoxContainer/CoopPrompt");
        _controllerPrompt = GetNode<Label>("BoxContainer/CenterContainer/ControllerPrompt");

        _resume.Pressed += OnResume;
        _coopPrompt.Pressed += OnCoopPrompt;
        GetNode<Button>("BoxContainer/ExitToOS").Pressed += OnExitToOS;

        CoopManager.Ref().CoopChanged += OnCoopChanged;
        CoopManager.Ref().PlayerControllerActiveChanged += OnPlayerControllerActiveChanged;
    }

    public void OnResume() {
        ClearControllerPrompt();
        Close();
    }

    public void OnCoopPrompt() {
        CoopManager.Ref().EnableCoop();
    }

    public void OnExitToOS() {
        GetTree().Quit();
    }

    private void UpdateCoopPrompt() {
        if (_coopPrompt == null) {
            throw new System.Exception("CoopPrompt must be set before UpdateCoopPrompt is called");
        }

        bool disabled = false;
        if (CoopManager.Ref().CoopActive) {
            _coopPrompt.Text = "Disable Co-op";
        }
        else {
            if (CoopManager.Ref().IsPlayerControllerActive(CoopManager.PlayerID.Two)) {
                _coopPrompt.Text = "Enable Co-op";
            }
            else {
                _coopPrompt.Text = "Connect 2nd Controller";
                disabled = true;
            }
        }
        _coopPrompt.Disabled = disabled;
    }

    public void ClearControllerPrompt() {
        if (_controllerPrompt == null) {
            throw new System.Exception("ControllerPrompt must be set before OnJoyConnectionChanged is called");
        }
        _controllerPrompt.Text = "";
    }

    public void UpdateControllerPrompt() {
        if (_controllerPrompt == null) {
            throw new System.Exception("ControllerPrompt must be set before OnJoyConnectionChanged is called");
        }

        string text = "";
        CoopManager.PlayerID[] playerIDs = new CoopManager.PlayerID[] {
            CoopManager.PlayerID.One,
            CoopManager.PlayerID.Two
        };
        foreach (var playerID in playerIDs) {
            if (CoopManager.Ref().IsPlayerControllerActive(playerID)) {
                continue;
            }
            text += $"Player {((int)playerID) + 1} Controller Disconnected\n";
        }
    }

    public void OnCoopChanged(bool coopActive) {
        UpdateCoopPrompt();

        if (_resume == null) {
            throw new System.Exception("Resume button null");
        }
        if (coopActive && !CoopManager.Ref().IsPlayerControllerActive(CoopManager.PlayerID.Two)) {
            _resume.Disabled = true;
        }
        else {
            _resume.Disabled = false;
        }
    }

    public void OnPlayerControllerActiveChanged(CoopManager.PlayerID playerID, bool connected) {
        UpdateCoopPrompt();
        UpdateControllerPrompt();
    }
}

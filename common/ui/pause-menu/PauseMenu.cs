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

        Player[] players = DependencyInjector.Ref().GetPlayers();
        _closeActions.Add("pause_menu");
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
        RequestClose();
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
            if (CoopManager.Ref().CanEnableCoop()) {
                _coopPrompt.Text = "Enable Co-op";
            }
            else {
                _coopPrompt.Text = "Co-op\nConnect a controller for each player";
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
        foreach (var playerID in CoopManager.Ref().PlayerIDs) {
            if (CoopManager.Ref().IsPlayerControllerActive(playerID)) {
                continue;
            }
            text += $"Player {(int)playerID} Controller Disconnected\n";
        }
    }

    public void OnCoopChanged(bool coopActive) {
        UpdateControllerPrompt();
        UpdateCoopPrompt();
    }

    public void OnPlayerControllerActiveChanged(CoopManager.PlayerID playerID, bool connected) {
        UpdateControllerPrompt();
        UpdateCoopPrompt();
    }
}

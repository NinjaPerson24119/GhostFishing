using Godot;
using System.Collections.Generic;

internal partial class PauseMenu : Menu {
    private Button? _resume;
    private Button? _coopPrompt;
    private Label? _controllerPrompt;

    public PauseMenu() {
        FocusMode = FocusModeEnum.All;
    }

    public override void _Ready() {
        base._Ready();

        SetCloseActions();

        _resume = GetNode<Button>("BoxContainer/Resume");
        _coopPrompt = GetNode<Button>("BoxContainer/CoopPrompt");
        _controllerPrompt = GetNode<Label>("BoxContainer/CenterContainer/ControllerPrompt");

        _resume.Pressed += OnResume;
        _coopPrompt.Pressed += OnCoopPrompt;
        GetNode<Button>("BoxContainer/ExitToOS").Pressed += OnExitToOS;

        PlayerManager.Ref().CoopChanged += OnCoopChanged;
        PlayerManager.Ref().PlayerControllerActiveChanged += OnPlayerControllerActiveChanged;
        PlayerManager.Ref().PlayerActiveChanged += OnPlayerActiveChanged;
    }

    public override void Open() {
        UpdateCoopPrompt();
        UpdateControllerPrompt();
        base.Open();
    }

    public void OnResume() {
        ClearControllerPrompt();
        RequestClose();
    }

    public void OnCoopPrompt() {
        PlayerManager.Ref().EnableCoop();
    }

    public void OnExitToOS() {
        GetTree().Quit();
    }

    private void UpdateCoopPrompt() {
        if (_coopPrompt == null) {
            throw new System.Exception("CoopPrompt must be set before UpdateCoopPrompt is called");
        }

        bool disabled = false;
        if (PlayerManager.Ref().CoopActive) {
            _coopPrompt.Text = "Disable Co-op";
        }
        else {
            if (PlayerManager.Ref().CanEnableCoop()) {
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
        foreach (var playerID in PlayerManager.PlayerIDs) {
            if (PlayerManager.Ref().IsPlayerControllerActive(playerID)) {
                continue;
            }
            text += $"Player {playerID.PlayerNumber()} Controller Disconnected\n";
        }
        _controllerPrompt.Text = text;
    }

    public void SetCloseActions() {
        _closeActions.Clear();
        _closeActions.Add("pause_menu");
        Dictionary<PlayerID, Player> players = PlayerInjector.Ref().GetPlayers();
        foreach (var kv in players) {
            Player player = kv.Value;
            if (player.PlayerContext == null) {
                continue;
            }
            _closeActions.Add(player.PlayerContext.ActionCancel);
        }
    }

    public void OnCoopChanged(bool coopActive) {
        UpdateControllerPrompt();
        UpdateCoopPrompt();
    }

    public void OnPlayerControllerActiveChanged(PlayerID playerID, bool connected) {
        UpdateControllerPrompt();
        UpdateCoopPrompt();
    }

    public void OnPlayerActiveChanged(PlayerID playerID, bool active) {
        SetCloseActions();
    }
}

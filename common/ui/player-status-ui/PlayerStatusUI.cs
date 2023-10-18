using Godot;
using System;

public partial class PlayerStatusUI : PseudoFocusControl {
    PlayerContext? _playerContext;

    public override void _Ready() {
        _playerContext = DependencyInjector.Ref().GetLocalPlayerContext(GetPath());
        if (_playerContext == null) {
            throw new Exception("PlayerContext null");
        }

        Label label = GetNode<Label>("Label");
        int playerNumber = _playerContext.PlayerID.PlayerNumber();
        label.Text = $"Player {playerNumber}";
    }
}

using Godot;

internal partial class PauseMenu : Menu {
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
    }
}

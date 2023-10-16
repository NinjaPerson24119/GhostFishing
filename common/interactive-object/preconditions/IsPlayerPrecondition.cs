public class IsPlayerPrecondition : IInteractiveObjectPrecondition {
    CoopManager.PlayerID _playerID;
    public IsPlayerPrecondition(CoopManager.PlayerID playerID) {
        _playerID = playerID;
    }

    public bool Check(InteractiveObject interactiveObject, Player player) {
        if (player.PlayerContext == null) {
            throw new System.Exception("PlayerContext is null");
        }
        return _playerID == player.PlayerContext.PlayerID;
    }
}

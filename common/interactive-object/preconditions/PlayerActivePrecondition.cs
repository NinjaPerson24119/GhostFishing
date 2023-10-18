public class PlayerActivePrecondition : IInteractiveObjectPrecondition {
    PlayerID _playerID;
    public PlayerActivePrecondition(PlayerID playerID) {
        _playerID = playerID;
    }

    public bool Check(InteractiveObject interactiveObject, Player player) {
        return CoopManager.Ref().IsPlayerActive(_playerID);
    }
}

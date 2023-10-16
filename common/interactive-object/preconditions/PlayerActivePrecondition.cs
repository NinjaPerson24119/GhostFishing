public class PlayerActivePrecondition : IInteractiveObjectPrecondition {
    CoopManager.PlayerID _playerID;
    public PlayerActivePrecondition(CoopManager.PlayerID playerID) {
        _playerID = playerID;
    }

    public bool Check(InteractiveObject interactiveObject, Player player) {
        return CoopManager.Ref().IsPlayerActive(_playerID);
    }
}

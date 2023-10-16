using Godot;

public partial class CoopManager : Node {
    public enum PlayerID {
        Invalid = -1,
        One = 1,
        Two = 2,
    }

    static SingletonTracker<CoopManager> _singletonTracker = new SingletonTracker<CoopManager>();
    private static CoopManager _singleton { get => _singletonTracker.Ref(); }

    public CoopManager() {
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready() {
        _singletonTracker.Ready(this);
    }
    public static CoopManager Ref() {
        return _singleton;
    }

    public const int MaxPlayers = 2;

    public bool IsPlayerActive(PlayerID playerID) {
        switch (playerID) {
            case PlayerID.One:
                return true;
            case PlayerID.Two:
                return Input.GetConnectedJoypads().Count > 1;
            default:
                throw new System.Exception($"Invalid player ID {playerID}");
        }
    }
}

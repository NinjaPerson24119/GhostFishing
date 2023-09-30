using Godot;

public class PlayerState {
    public Inventory PlayerInventory;
}

public partial class PersistentState : Node {
    static SingletonTracker<PersistentState> _singletonTracker = new SingletonTracker<PersistentState>();
    private static PersistentState _singleton { get => _singletonTracker.Ref(); }
    public static PersistentState Ref() {
        return _singleton;
    }

    private CoopManager _coopManager;

    // TODO: expose safe way to access player states within viewports
    private PlayerState[] _playerStates;

    public override void _Ready() {
        _singletonTracker.Ready(this);

        int noPlayers = CoopManager.Ref().NoPlayers;
        _playerStates = new PlayerState[noPlayers];
        for (int i = 0; i < noPlayers; i++) {
            _playerStates[i] = new PlayerState() {
                PlayerInventory = AssetManager.Ref().DefaultInventory,
            };
        }
    }
}

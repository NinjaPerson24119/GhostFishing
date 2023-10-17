using Godot;

public partial class CoopManager : Node {
    public enum PlayerID {
        Invalid = -1,
        One = 1,
        Two = 2,
    }
    public const int MaxPlayers = 2;

    static SingletonTracker<CoopManager> _singletonTracker = new SingletonTracker<CoopManager>();
    private static CoopManager _singleton { get => _singletonTracker.Ref(); }
    public static CoopManager Ref() {
        return _singleton;
    }

    public bool CoopActive { get; private set; } = false;

    [Signal]
    public delegate void CoopChangedEventHandler(bool coopActive);
    [Signal]
    public delegate void PlayerControllerActiveChangedEventHandler(PlayerID playerID, bool active);

    public CoopManager() {
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready() {
        _singletonTracker.Ready(this);
        Input.JoyConnectionChanged += OnJoyConnectionChanged;
    }

    public bool IsPlayerControllerActive(PlayerID playerID) {
        var connectedJoypads = Input.GetConnectedJoypads();
        switch (playerID) {
            case PlayerID.One:
                return connectedJoypads.Contains(0);
            case PlayerID.Two:
                return connectedJoypads.Contains(1);
            default:
                throw new System.Exception($"Invalid player ID {playerID}");
        }
    }

    public bool IsPlayerActive(PlayerID playerID) {
        return playerID == PlayerID.One || (playerID == PlayerID.Two && CoopActive);
    }

    public void OnJoyConnectionChanged(long device, bool connected) {
        if (device == 1 && connected == false) {
            GD.Print("Player 2 disconnected.");
            DisableCoop();
        }

        switch (device) {
            case 0:
                EmitSignal(SignalName.PlayerControllerActiveChanged, (int)PlayerID.One, connected);
                break;
            case 1:
                EmitSignal(SignalName.PlayerControllerActiveChanged, (int)PlayerID.Two, connected);
                break;
        }
    }

    public void EnableCoop() {
        if (CoopActive) {
            return;
        }
        if (!IsPlayerControllerActive(PlayerID.Two)) {
            GD.Print("Can't enable co-op because Player 2 controller not connected.");
            return;
        }
        CoopActive = true;

        Input.MouseMode = Input.MouseModeEnum.Hidden;

        EmitSignal(SignalName.CoopChanged, CoopActive);
        GD.Print($"Co-op enabled.");
    }

    public void DisableCoop() {
        if (!CoopActive) {
            return;
        }
        CoopActive = false;

        Input.MouseMode = Input.MouseModeEnum.Visible;

        EmitSignal(SignalName.CoopChanged, CoopActive);
        GD.Print($"Co-op disabled.");
    }
}

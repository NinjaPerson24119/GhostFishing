using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class CoopManager : Node {
    public enum PlayerID {
        Invalid = -1,
        One = 1,
        Two = 2,
    }
    public readonly PlayerID[] PlayerIDs = new PlayerID[] {
        PlayerID.One,
        PlayerID.Two
    };

    static SingletonTracker<CoopManager> _singletonTracker = new SingletonTracker<CoopManager>();
    private static CoopManager _singleton { get => _singletonTracker.Ref(); }
    public static CoopManager Ref() {
        return _singleton;
    }

    public bool CoopActive { get; private set; } = false;
    private Dictionary<PlayerID, long> _playerDevices = new Dictionary<PlayerID, long>();

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

        // simulate initial controller connections
        var devices = Input.GetConnectedJoypads();
        for (int i = 0; i < devices.Count; i++) {
            OnJoyConnectionChanged(devices[i], true);
        }
    }

    public bool IsPlayerControllerActive(PlayerID playerID) {
        return _playerDevices.ContainsKey(playerID);
    }

    public bool IsPlayerActive(PlayerID playerID) {
        return playerID == PlayerID.One || (playerID == PlayerID.Two && CoopActive);
    }

    public long? GetPlayerControllerDevice(PlayerID playerID) {
        if (_playerDevices.ContainsKey(playerID)) {
            return _playerDevices[playerID];
        }
        return null;
    }

    public void OnJoyConnectionChanged(long device, bool connected) {
        if (connected) {
            foreach (PlayerID id in PlayerIDs) {
                if (_playerDevices.ContainsValue(device)) {
                    continue;
                }
                if (!_playerDevices.ContainsKey(id)) {
                    _playerDevices.Add(id, device);
                    GD.Print($"Player {(int)id} connected.");
                    EmitSignal(SignalName.PlayerControllerActiveChanged, (int)id, connected);
                    return;
                }
            }
        }
        else {
            try {
                var kv = _playerDevices.First(kvp => kvp.Value == device);
                PlayerID playerID = kv.Key;
                _playerDevices.Remove(playerID);
                GD.Print($"Player {(int)playerID} disconnected.");
                EmitSignal(SignalName.PlayerControllerActiveChanged, (int)playerID, connected);
            }
            catch { }
        }
    }

    public bool CanEnableCoop() {
        if (CoopActive) {
            return false;
        }
        foreach (var id in PlayerIDs) {
            if (!IsPlayerControllerActive(id)) {
                return false;
            }
        }
        return true;
    }

    public void EnableCoop() {
        if (!CanEnableCoop()) {
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

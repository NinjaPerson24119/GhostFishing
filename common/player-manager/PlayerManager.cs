using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerManager : Node {
    public static PlayerID[] PlayerIDs = new PlayerID[] {
        PlayerID.One,
        PlayerID.Two
    };

    static SingletonTracker<PlayerManager> _singletonTracker = new SingletonTracker<PlayerManager>();
    private static PlayerManager _singleton { get => _singletonTracker.Ref(); }
    public static PlayerManager Ref() {
        return _singleton;
    }

    public bool CoopActive { get; private set; } = false;
    private Dictionary<PlayerID, long> _playerDevices = new Dictionary<PlayerID, long>();

    [Signal]
    public delegate void CoopChangedEventHandler(bool coopActive);
    [Signal]
    public delegate void PlayerControllerActiveChangedEventHandler(PlayerID playerID, bool active);
    [Signal]
    public delegate void PlayerActiveChangedEventHandler(PlayerID playerID, bool active);

    public PlayerManager() {
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
                    GD.Print($"Player {id.PlayerNumber()} connected.");
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
                GD.Print($"Player {playerID.PlayerNumber()} disconnected.");
                EmitSignal(SignalName.PlayerControllerActiveChanged, (int)playerID, connected);
            }
            catch { }
        }
    }

    public bool CanUpdateCoop() {
        // defer to SaveStateManager since if we can't save, we definitely don't want to respawn the players
        if (SaveStateManager.Ref().Locked) {
            return false;
        }
        return true;
    }

    public bool CanEnableCoop() {
        if (CoopActive || !CanUpdateCoop()) {
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
            GD.Print("Failed to enable co-op. Because preconditions were not met.");
            return;
        }
        CoopActive = true;

        Input.MouseMode = Input.MouseModeEnum.Hidden;

        EmitSignal(SignalName.PlayerActiveChanged, (int)PlayerID.Two, true);
        EmitSignal(SignalName.CoopChanged, CoopActive);
        GD.Print($"Co-op enabled.");
    }

    public void DisableCoop() {
        if (!CoopActive || !CanUpdateCoop()) {
            return;
        }
        CoopActive = false;

        Input.MouseMode = Input.MouseModeEnum.Visible;

        EmitSignal(SignalName.PlayerActiveChanged, (int)PlayerID.Two, false);
        EmitSignal(SignalName.CoopChanged, CoopActive);
        GD.Print($"Co-op disabled.");
    }
}

using Godot;
using System.Collections.Generic;

public partial class CoopManager : Node {
    public enum PlayerID {
        Invalid = -1,
        One = 1,
        Two = 2,
    }
    public const int MaxPlayers = 1;

    private struct PlayerControllerMapping {
        public PlayerID PlayerID;
        public long? Device;
    }

    static SingletonTracker<CoopManager> _singletonTracker = new SingletonTracker<CoopManager>();
    private static CoopManager _singleton { get => _singletonTracker.Ref(); }
    public static CoopManager Ref() {
        return _singleton;
    }

    public bool CoopActive { get; private set; } = false;
    private PlayerControllerMapping[] _playerDevices = {
        new PlayerControllerMapping() {
            PlayerID = PlayerID.One,
            Device = null,
        },
        new PlayerControllerMapping() {
            PlayerID = PlayerID.Two,
            Device = null,
        },
    };

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

        var devices = Input.GetConnectedJoypads();
        for (int i = 0; i < devices.Count; i++) {
            OnJoyConnectionChanged(devices[i], true);
        }
    }

    public bool IsPlayerControllerActive(PlayerID playerID) {
        int p = (int)playerID;
        if (p < 1 || p > MaxPlayers) {
            return false;
        }
        int device = _playerDevices[p-1];

        return true;
    }

    public bool IsPlayerActive(PlayerID playerID) {
        return playerID == PlayerID.One || (playerID == PlayerID.Two && CoopActive);
    }

    public int? GetPlayerDevice(PlayerID playerID) {
        if (_playerDevices.ContainsKey(playerID)) {
            return _playerDevices[playerID];
        }
        return null;
    }

    public void OnJoyConnectionChanged(long device, bool connected) {
        for (int i = 0; i < MaxPlayers; i++) {

        }

        if (connected) {
            if (_playerDevices.ContainsKey((PlayerID)i)) {

            }
        }
        else {
            if (_playerDevices.ContainsValue((int)device)) {
                _playerDevices
                    _playerDevices.Remove((PlayerID)device);
            }
        }

        for (int i = 0; i < MaxPlayers; i++) {
            if () {

            }

            if (connected) {
                if (!_playerDevices.ContainsValue((int)device)) {
                    _playerDevices.Add((PlayerID)i, (int)device);
                    GD.Print($"Player {i + 1} connected.");
                }
            }
            else {
                if (_playerDevices.ContainsValue(i)) {
                    _playerDevices.Remove((PlayerID)i);
                    GD.Print($"Player {i + 1} disconnected.");
                }
            }
        }

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

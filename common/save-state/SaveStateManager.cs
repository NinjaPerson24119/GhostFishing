using System.Text.Json;
using System.IO;
using Godot;
using System;
using System.Collections.Generic;

internal partial class SaveStateManager : Node {
    public class Lock : IDisposable {
        private SaveStateManager _saveStateManager;
        private bool _released;
        public Lock(SaveStateManager saveStateManager) {
            _saveStateManager = saveStateManager;
            saveStateManager._locks++;
        }

        ~Lock() {
            if (!_released) {
                GD.PrintErr("SaveStateManagerLock was not released before being destroyed.");
                Dispose();
            }
        }

        public void Dispose() {
            if (!_released) {
                _saveStateManager._locks--;
                _released = true;
            }
        }
    }

    static SingletonTracker<SaveStateManager> _singletonTracker = new SingletonTracker<SaveStateManager>();
    public static SaveStateManager Ref() {
        return _singletonTracker.Ref();
    }

    private string _saveStatePath = ProjectSettings.GlobalizePath("user://save-state.json");
    private int _locks = 0;
    private const int VERSION = 0;

    [Signal]
    public delegate void LoadedSaveStateEventHandler();

    public SaveStateManager() {
        ProcessMode = ProcessModeEnum.Always;
    }

    ~SaveStateManager() {
        if (_locks > 0) {
            GD.PrintErr("SaveStateManager was not closed before being destroyed.");
        }
    }

    public override void _Ready() {
        _singletonTracker.Ready(this);
    }

    public override void _Input(InputEvent inputEvent) {
        if (inputEvent.IsActionPressed("save_game")) {
            Save();
        }
        if (inputEvent.IsActionPressed("load_game")) {
            Load();
        }
    }

    private SaveState CaptureState() {
        SaveState saveState = new SaveState() {
            Version = VERSION,
            CommonSaveState = new CommonSaveState() {
                GameSeconds = GameClock.GameSeconds % GameClock.SecondsPerDay,
            },
            PlayerSaveState = new Dictionary<PlayerID, PlayerSaveState>(),
            InventoryStates = AssetManager.Ref().GetInventoryInstanceDTOs(),
        };

        Dictionary<PlayerID, Player> players = PlayerInjector.Ref().GetPlayers();
        foreach (var kv in players) {
            PlayerStateView view = AssetManager.Ref().GetPlayerView(kv.Key);
            saveState.PlayerSaveState.Add(kv.Key, new PlayerSaveState() {
                GlobalPositionX = view.GlobalPosition.X,
                GlobalPositionZ = view.GlobalPosition.Z,
                GlobalRotationY = view.GlobalRotation.Y,
            });
            if (view.CameraState != null) {
                saveState.PlayerSaveState[kv.Key].CameraState = view.CameraState.Value;
            }
        }
        return saveState;
    }

    void Save() {
        if (_locks > 0) {
            GD.PrintErr("Cannot save. SaveStateManager is locked.");
            return;
        }
        SaveState saveState;
        try {
            saveState = CaptureState();
        }
        catch (Exception e) {
            GD.PrintErr($"Cannot save. Error capturing state: {e}");
            return;
        }

        if (saveState.PlayerSaveState == null) {
            GD.PrintErr("PlayerSaveState is null");
            return;
        }
        string jsonString = JsonSerializer.Serialize<SaveState>(saveState, new JsonSerializerOptions() {
            WriteIndented = true,
        });
        File.WriteAllText(_saveStatePath, jsonString);
        GD.Print($"Saved state to {_saveStatePath}");
    }

    void SetState(SaveState saveState) {
        if (saveState.Version != VERSION) {
            GD.PrintErr($"Save state version {saveState.Version} does not match expected version {VERSION}");
            return;
        }
        if (saveState.CommonSaveState != null) {
            GameClock.GameSeconds = saveState.CommonSaveState.GameSeconds;
        }


        if (saveState.PlayerSaveState != null) {
            Dictionary<PlayerID, Player> players = PlayerInjector.Ref().GetPlayers();
            foreach (var kv in players) {
                PlayerSaveState playerSaveState = saveState.PlayerSaveState[kv.Key];
                Player player = kv.Value;

                PlayerStateView view = AssetManager.Ref().GetPlayerView(kv.Key);

                view.GlobalPosition = new Vector3(playerSaveState.GlobalPositionX, 0, playerSaveState.GlobalPositionZ);
                view.GlobalRotation = new Vector3(0, playerSaveState.GlobalRotationY, 0);
                player.ResetAboveWater();

                view.CameraState = playerSaveState.CameraState;
            }
        }
        if (saveState.InventoryStates != null) {
            GD.Print($"Setting inventory states: {saveState.InventoryStates.Count}");
            AssetManager.Ref().SetInventoryInstanceDTOs(saveState.InventoryStates);
        }
    }

    void Load() {
        if (_locks > 0) {
            GD.PrintErr("Cannot load save. SaveStateManager is locked.");
            return;
        }

        string jsonString;
        try {
            jsonString = File.ReadAllText(_saveStatePath);
        }
        catch (Exception e) {
            GD.PrintErr($"Cannot load save. Error reading file: {e}");
            return;
        }

        SaveState? saveState = JsonSerializer.Deserialize<SaveState>(jsonString);
        if (saveState == null) {
            GD.PrintErr("Cannot load save. Deserialized null.");
            return;
        }
        SetState(saveState);
        GD.Print($"Loaded state from {_saveStatePath}");
        EmitSignal(SignalName.LoadedSaveState);
    }

    public Lock GetLock() {
        return new Lock(this);
    }
}

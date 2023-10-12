using System.Text.Json;
using System.IO;
using Godot;
using System;

public partial class SaveStateManager : Node {
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
    private int _noPlayers;
    private int _locks = 0;

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
        _noPlayers = CoopManager.Ref().NoPlayers;
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
            CommonSaveState = new CommonSaveState() {
                GameSeconds = GameClock.GameSeconds % GameClock.SecondsPerDay,
            },
            PlayerSaveState = new PlayerSaveState[_noPlayers],
            InventoryStates = AssetManager.Ref().GetInventoryDTOs(),
        };

        for (int i = 0; i < _noPlayers; i++) {
            Player player = DependencyInjector.Ref().GetPlayer();
            saveState.PlayerSaveState[i] = new PlayerSaveState() {
                GlobalPositionX = player.GlobalPosition.X,
                GlobalPositionZ = player.GlobalPosition.Z,
                GlobalRotationY = player.GlobalRotation.Y,
            };
        }
        return saveState;
    }

    void Save() {
        if (_locks > 0) {
            GD.Print("Cannot save. SaveStateManager is locked.");
            return;
        }

        SaveState saveState = CaptureState();
        if (saveState.PlayerSaveState == null) {
            GD.Print("PlayerSaveState is null");
            return;
        }
        GD.Print(saveState.PlayerSaveState[0].GlobalPositionX);
        string jsonString = JsonSerializer.Serialize<SaveState>(saveState, new JsonSerializerOptions() {
            WriteIndented = true,
        });
        File.WriteAllText(_saveStatePath, jsonString);
        GD.Print($"Saved state to {_saveStatePath}");
    }

    void SetState(SaveState saveState) {
        if (saveState.CommonSaveState != null) {
            GameClock.GameSeconds = saveState.CommonSaveState.GameSeconds;
        }
        if (saveState.PlayerSaveState != null) {
            for (int i = 0; i < saveState.PlayerSaveState.Length; i++) {
                PlayerSaveState playerState = saveState.PlayerSaveState[i];
                Player player = DependencyInjector.Ref().GetPlayer();
                player.ResetAboveWater(true, new Vector2(playerState.GlobalPositionX, playerState.GlobalPositionZ), playerState.GlobalRotationY);
            }
        }
        if (saveState.InventoryStates != null) {
            GD.Print($"Setting inventory states: {saveState.InventoryStates.Count}");
            AssetManager.Ref().SetInventoryDTOs(saveState.InventoryStates);
        }
    }

    void Load() {
        if (_locks > 0) {
            GD.Print("Cannot load save. SaveStateManager is locked.");
            return;
        }

        string jsonString = File.ReadAllText(_saveStatePath);
        SaveState? saveState = JsonSerializer.Deserialize<SaveState>(jsonString);
        if (saveState == null) {
            GD.Print("Cannot load save. Deserialized null.");
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

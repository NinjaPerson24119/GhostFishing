using System.Text.Json;
using System.IO;
using Godot;

public partial class SaveStateManager : Node {
    static SingletonTracker<SaveStateManager> _singletonTracker = new SingletonTracker<SaveStateManager>();

    private string _saveStatePath = ProjectSettings.GlobalizePath("user://save-state.json");
    private int _noPlayers;

    public SaveStateManager() {
        ProcessMode = ProcessModeEnum.Always;
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
    }

    void Load() {
        string jsonString = File.ReadAllText(_saveStatePath);
        SaveState? saveState = JsonSerializer.Deserialize<SaveState>(jsonString);
        if (saveState == null) {
            GD.Print("Cannot load save. Deserialized null.");
            return;
        }
        SetState(saveState);
        GD.Print($"Loaded state from {_saveStatePath}");
    }
}

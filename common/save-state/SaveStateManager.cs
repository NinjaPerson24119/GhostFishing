using System.Text.Json;
using System.IO;
using Godot;

public partial class SaveStateManager : Node {
    static SingletonTracker<SaveStateManager> _singletonTracker = new SingletonTracker<SaveStateManager>();
    public override void _Ready() {
        _singletonTracker.Ready(this);
    }

    private string _saveStatePath = ProjectSettings.GlobalizePath("user://save-state.json");
    private SaveState _saveState;

    const int noPlayers = 1;

    public override void _Input(InputEvent inputEvent) {
        if (inputEvent.IsActionPressed("save_game")) {
            Save();
        }
        if (inputEvent.IsActionPressed("load_game")) {
            Load();
        }
    }

    void CaptureState() {
        _saveState = new SaveState() {
            CommonSaveState = new CommonSaveState() {
                GameSeconds = GameClock.GameSeconds % GameClock.SecondsPerDay,
            },
            PlayerSaveState = new PlayerSaveState[noPlayers],
        };

        for (int i = 0; i < noPlayers; i++) {
            Player player = DependencyInjector.Ref().GetPlayer();
            _saveState.PlayerSaveState[i] = new PlayerSaveState() {
                GlobalPositionX = player.GlobalPosition.X,
                GlobalPositionZ = player.GlobalPosition.Z,
                GlobalRotationY = player.GlobalRotation.Y,
            };
        }
    }

    void Save() {
        CaptureState();
        GD.Print(_saveState.PlayerSaveState[0].GlobalPositionX);
        string jsonString = JsonSerializer.Serialize<SaveState>(_saveState, new JsonSerializerOptions() {
            WriteIndented = true,
        });
        File.WriteAllText(_saveStatePath, jsonString);
        GD.Print($"Saved state to {_saveStatePath}");
    }

    void SetState() {
        GameClock.GameSeconds = _saveState.CommonSaveState.GameSeconds;
        for (int i = 0; i < noPlayers; i++) {
            PlayerSaveState playerState = _saveState.PlayerSaveState[i];
            Player player = DependencyInjector.Ref().GetPlayer();
            player.ResetAboveWater(true, new Vector2(playerState.GlobalPositionX, playerState.GlobalPositionZ), playerState.GlobalRotationY);
        }
    }

    void Load() {
        string jsonString = File.ReadAllText(_saveStatePath);
        _saveState = JsonSerializer.Deserialize<SaveState>(jsonString);
        SetState();
        GD.Print($"Loaded state from {_saveStatePath}");
    }
}

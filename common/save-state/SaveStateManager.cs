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
    const string playerNodePath = "/root/Main/Player";

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
            CommonState = new CommonState() {
                GameSeconds = GameClock.GameSeconds % GameClock.SecondsPerDay,
            },
            PlayerState = new PlayerState[noPlayers],
        };

        for (int i = 0; i < noPlayers; i++) {
            Player player = GetNode<Player>(playerNodePath);
            _saveState.PlayerState[i] = new PlayerState() {
                GlobalPositionX = player.GlobalPosition.X,
                GlobalPositionZ = player.GlobalPosition.Z,
                GlobalRotationY = player.GlobalRotation.Y,
            };
        }
    }

    void Save() {
        CaptureState();
        GD.Print(_saveState.PlayerState[0].GlobalPositionX);
        string jsonString = JsonSerializer.Serialize<SaveState>(_saveState, new JsonSerializerOptions() {
            WriteIndented = true,
        });
        File.WriteAllText(_saveStatePath, jsonString);
        GD.Print($"Saved state to {_saveStatePath}");
    }

    void SetState() {
        GameClock.GameSeconds = _saveState.CommonState.GameSeconds;
        for (int i = 0; i < noPlayers; i++) {
            PlayerState playerState = _saveState.PlayerState[i];
            Player player = GetNode<Player>(playerNodePath);
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

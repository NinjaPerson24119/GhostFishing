using Godot;
using System.Collections.Generic;

public partial class PlayerContext : Node {
    [Export]
    public CoopManager.PlayerID PlayerID { get; private set; } = CoopManager.PlayerID.Invalid;
    [Export]
    public Vector3 InitialGlobalPosition { get; private set; } = Vector3.Zero;

    public PlayerStateView PlayerStateView {
        get => AssetManager.Ref().GetPlayerView(0);
    }
    public Player Player {
        get => GetNode<Player>("Player");
    }
    public FollowCamera FollowCamera {
        get => GetNode<FollowCamera>("FollowCamera");
    }
    public PlayerMenu PlayerMenu {
        get => GetNode<PlayerMenu>("PlayerHUD/PlayerMenu");
    }
    public PlayerController Controller {
        get => GetNode<PlayerController>("PlayerController");
    }

    public Vector2 MovementControlVector() {
        int device = (int)PlayerID;
        return Input.GetVector(
            $"turn_left_{device}",
            $"turn_right_{device}",
            $"move_backward_{device}",
            $"move_forward_{device}"
        );
    }

    public List<string> MovementActions() {
        int device = (int)PlayerID;
        return new List<string> {
            $"turn_left_{device}",
            $"turn_right_{device}",
            $"move_backward_{device}",
            $"move_forward_{device}"
        };
    }

    public Vector2 CameraControlVector() {
        int device = (int)PlayerID;
        return Input.GetVector(
            $"rotate_camera_left_{device}",
            $"rotate_camera_right_{device}",
            $"rotate_camera_down_{device}",
            $"rotate_camera_up_{device}"
        );
    }
    public string ActionCycleZoom {
        get => $"cycle_zoom_{(int)PlayerID}";
    }

    public string ActionCancel {
        get => $"cancel_{(int)PlayerID}";
    }
    public string ActionSelect {
        get => $"select_{(int)PlayerID}";
    }

    public string ActionOpenInventory {
        get => $"open_inventory_{(int)PlayerID}";
    }
    public string ActionRotateClockwise {
        get => $"inventory_rotate_clockwise_{(int)PlayerID}";
    }
    public string ActionRotateCounterClockwise {
        get => $"inventory_rotate_counterclockwise_{(int)PlayerID}";
    }

    public string ActionNavigateUp {
        get => $"navigate_up_{(int)PlayerID}";
    }
    public string ActionNavigateDown {
        get => $"navigate_down_{(int)PlayerID}";
    }
    public string ActionNavigateLeft {
        get => $"navigate_left_{(int)PlayerID}";
    }
    public string ActionNavigateRight {
        get => $"navigate_right_{(int)PlayerID}";
    }
}

using Godot;
using System.Collections.Generic;

// indirection for player input
// this is required since each player has their own input mapping
public partial class PlayerContext : Node {
    public Vector2 MovementControlVector() {
        int device = PlayerID.PlayerControlMappingNumber();
        return Input.GetVector(
            $"turn_left_{device}",
            $"turn_right_{device}",
            $"move_backward_{device}",
            $"move_forward_{device}"
        );
    }

    public List<string> MovementActions() {
        int device = PlayerID.PlayerControlMappingNumber();
        return new List<string> {
            $"turn_left_{device}",
            $"turn_right_{device}",
            $"move_backward_{device}",
            $"move_forward_{device}"
        };
    }

    public Vector2 CameraControlVector() {
        int device = PlayerID.PlayerControlMappingNumber();
        return Input.GetVector(
            $"rotate_camera_left_{device}",
            $"rotate_camera_right_{device}",
            $"rotate_camera_down_{device}",
            $"rotate_camera_up_{device}"
        );
    }
    public string ActionCycleZoom {
        get => $"cycle_zoom_{PlayerID.PlayerControlMappingNumber()}";
    }

    public string ActionCancel {
        get => $"cancel_{PlayerID.PlayerControlMappingNumber()}";
    }
    public string ActionSelect {
        get => $"select_{PlayerID.PlayerControlMappingNumber()}";
    }

    public string ActionOpenInventory {
        get => $"open_inventory_{PlayerID.PlayerControlMappingNumber()}";
    }
    public string ActionRotateClockwise {
        get => $"inventory_rotate_clockwise_{PlayerID.PlayerControlMappingNumber()}";
    }
    public string ActionRotateCounterClockwise {
        get => $"inventory_rotate_counterclockwise_{PlayerID.PlayerControlMappingNumber()}";
    }
    public string ActionNextInventoryFrame {
        get => $"inventory_next_frame_{PlayerID.PlayerControlMappingNumber()}";
    }

    public string ActionNavigateUp {
        get => $"navigate_up_{PlayerID.PlayerControlMappingNumber()}";
    }
    public string ActionNavigateDown {
        get => $"navigate_down_{PlayerID.PlayerControlMappingNumber()}";
    }
    public string ActionNavigateLeft {
        get => $"navigate_left_{PlayerID.PlayerControlMappingNumber()}";
    }
    public string ActionNavigateRight {
        get => $"navigate_right_{PlayerID.PlayerControlMappingNumber()}";
    }
}

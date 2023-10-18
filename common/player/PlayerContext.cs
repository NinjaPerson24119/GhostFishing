using Godot;
using System.Collections.Generic;

public partial class PlayerContext : Node {
    [Export]
    public PlayerID PlayerID { get; private set; } = PlayerID.Invalid;
    [Export]
    public Vector3 InitialGlobalPosition { get; private set; } = Vector3.Zero;

    public PlayerStateView PlayerStateView {
        get => AssetManager.Ref().GetPlayerView(PlayerID);
    }
    public Player Player {
        get => GetNode<Player>("Player");
    }

    public FollowCamera FollowCamera {
        get {
            if (_usingSubviewport) {
                return GetNode<FollowCamera>("SubViewport/FollowCamera");
            }
            else {
                return GetNode<FollowCamera>("FollowCamera");
            }
        }
    }
    private bool _usingSubviewport = false;

    public PlayerMenu PlayerMenu {
        get => GetNode<PlayerMenu>("PseudoFocusContext/PlayerHUD/PlayerMenu");
    }
    public PlayerController Controller {
        get => GetNode<PlayerController>("PlayerController");
    }
    public SubViewport SubViewport {
        get {
            if (!_usingSubviewport) {
                throw new System.Exception("Subviewport is not in use, but detected a request for it");
            }
            return GetNode<SubViewport>("SubViewport");
        }
    }

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

    public override void _Ready() {
        PlayerManager.Ref().CoopChanged += OnCoopChanged;
    }

    public async void OnCoopChanged(bool coopActive) {
        FollowCamera followCamera = FollowCamera;
        if (coopActive && !_usingSubviewport) {
            followCamera.GetParent().RemoveChild(followCamera);
            // TODO: this might hang
            await ToSignal(followCamera, "tree_exited");
            SubViewport.AddChild(followCamera);
        }
        else if (!coopActive && _usingSubviewport) {
            followCamera.GetParent().RemoveChild(followCamera);
            // TODO: this might hang
            await ToSignal(followCamera, "tree_exited");
            AddChild(this);
        }
        _usingSubviewport = coopActive;
        SubViewport.ProcessMode = _usingSubviewport ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;


        if (PlayerID == PlayerID.Two) {
            Player.DisableControls = !coopActive;
            if (!coopActive) {
                PlayerMenu.Close();
            }
            PlayerMenu.Visible = coopActive;
            Controller.ProcessMode = coopActive ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
            FollowCamera.ProcessMode = coopActive ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        }
    }
}

using Godot;

public struct CameraStateDTO {
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float Distance { get; set; }
    public float CollidingMaxDistance { get; set; }
}

// this is serialized to the save state
public struct CameraState {
    public float Yaw { get; set; } = 0f;

    public float Pitch {
        get => _pitch;
        set {
            _pitch = Mathf.Clamp(value, _followCamera.MinPitch, _followCamera.MaxPitch);
        }
    }
    private float _pitch = Mathf.DegToRad(30f);

    public float Distance {
        get => _distance;
        set => _distance = Mathf.Clamp(value, _followCamera.MinDistance, _followCamera.MaxDistance);
    }
    private float _distance = 5;
    // sets the maximum distance based on a collision
    public float CollidingMaxDistance {
        get {
            return _collidingMaxDistance;
        }
        set {
            _collidingMaxDistance = Mathf.Max(_followCamera.RayNearDistance, value);
        }
    }
    private float _collidingMaxDistance = float.MaxValue;

    private FollowCamera _followCamera;
    public CameraState(FollowCamera followCamera, float distance) {
        _followCamera = followCamera;
        Distance = distance;
    }
    public CameraState(FollowCamera followCamera, CameraStateDTO dto) {
        _followCamera = followCamera;
        Yaw = dto.Yaw;
        Pitch = dto.Pitch;
        Distance = dto.Distance;
        CollidingMaxDistance = dto.CollidingMaxDistance;
    }

    public CameraStateDTO ToDTO() {
        return new CameraStateDTO {
            Yaw = Yaw,
            Pitch = Pitch,
            Distance = Distance,
            CollidingMaxDistance = CollidingMaxDistance,
        };
    }
}

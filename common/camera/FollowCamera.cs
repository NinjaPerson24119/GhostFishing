using Godot;

public partial class FollowCamera : Node3D {
    private struct CameraState {
        public float Yaw = 0f;

        public float Pitch {
            get => _pitch;
            set => _pitch = Mathf.Clamp(value, _followCamera.MinPitch, _followCamera.MaxPitch);
        }
        private float _pitch = Mathf.DegToRad(30f);

        public float Distance {
            get => _distance;
            set => _distance = Mathf.Clamp(value, _followCamera.MinDistance, _followCamera.MaxDistance);
        }
        private float _distance = 5;
        // sets the maximum distance based on a collision
        public float CollidingMaxDistance = float.MaxValue;

        private FollowCamera _followCamera;
        public CameraState(FollowCamera followCamera, float distance) {
            _followCamera = followCamera;
            Distance = distance;
        }
    }

    [Export]
    public float MinDistance = 4f;
    [Export]
    public float MaxDistance = 15f;
    [Export]
    private float CollidingDistanceBuffer = 0.2f;
    [Export]
    public float ZoomPerSecond = 20f;
    private enum ZoomEnum {
        In,
        Out,
    }
    private ZoomEnum _zoom = ZoomEnum.In;
    Timer _zoomTimer = new Timer() {
        WaitTime = 0.1f,
        OneShot = true,
    };
    private int _zoomStep = 1;
    private float? _zoomDistanceTarget;

    [Export]
    public float ControllerDegreesPerSecond {
        get => Mathf.RadToDeg(_controllerRadiansPerSecond);
        set => _controllerRadiansPerSecond = Mathf.DegToRad(value);
    }
    private float _controllerRadiansPerSecond = Mathf.DegToRad(120f);

    [Export]
    public float MinPitch = Mathf.DegToRad(15f);
    [Export]
    public float MaxPitch = Mathf.DegToRad(85f);

    [Export]
    public float MouseSensitivity = 0.005f;
    [Export]
    public float ResetRadiansPerSecond = Mathf.DegToRad(90f);

    [Export]
    public bool DisableControls = false;

    private Timer _cameraResetTimer = new Timer() {
        WaitTime = 3f,
        OneShot = true,
    };
    private bool IsCameraDefault {
        get => _isCameraDefault;
        set {
            _isCameraDefault = value;
        }
    }
    private bool _isCameraDefault = true;

    private Player? _player;
    private RayCast3D? _ray;
    private Area3D? _area3D;

    private CameraState _cameraState;
    private CameraState _lastCameraState;

    public FollowCamera() {
        float[] zoomSteps = GetZoomSteps();
        _cameraState = new CameraState(this, zoomSteps[_zoomStep]);
        _lastCameraState = _cameraState;
    }

    public override void _Ready() {
        _player = DependencyInjector.Ref().GetPlayer();
        _cameraState.Yaw = _player.GlobalRotation.Y;

        _ray = GetNode<RayCast3D>("RayCast3D");
        _area3D = GetNode<Area3D>("Area3D");

        AddChild(_cameraResetTimer);
        AddChild(_zoomTimer);
    }

    public override void _Input(InputEvent inputEvent) {
        if (DisableControls) {
            return;
        }

        if (inputEvent is InputEventMouseMotion mouseMotion) {
            _cameraState.Yaw -= mouseMotion.Relative.X * MouseSensitivity;
            _cameraState.Pitch += mouseMotion.Relative.Y * MouseSensitivity;
            IsCameraDefault = false;
            _cameraResetTimer.Start();
        }
        var mouseButtonEvent = inputEvent as InputEventMouseButton;
        if (mouseButtonEvent != null && mouseButtonEvent.Pressed) {
            switch (mouseButtonEvent.ButtonIndex) {
                case MouseButton.WheelUp:
                    _zoom = ZoomEnum.In;
                    _zoomTimer.Start();
                    _zoomDistanceTarget = null;
                    break;
                case MouseButton.WheelDown:
                    _zoom = ZoomEnum.Out;
                    _zoomTimer.Start();
                    _zoomDistanceTarget = null;
                    break;
            }
        }

        if (inputEvent.IsActionPressed("cycle_zoom")) {
            _zoomTimer.Stop();
            float[] zoomSteps = GetZoomSteps();
            _zoomStep = (_zoomStep + 1) % zoomSteps.Length;
            _zoomDistanceTarget = zoomSteps[_zoomStep];
            _cameraResetTimer.Start();
        }
    }

    public float[] GetZoomSteps() {
        return new float[] {
            MinDistance,
            MinDistance + (MaxDistance - MinDistance) / 3f,
            MinDistance + (MaxDistance - MinDistance) * 2f / 3f,
            MaxDistance,
        };
    }

    public override void _PhysicsProcess(double delta) {
        if (_ray == null) {
            throw new System.Exception("Ray is null");
        }
        if (_player == null) {
            throw new System.Exception("Player is null");
        }
        if (_area3D == null) {
            throw new System.Exception("Area3D is null");
        }

        _ray.TargetPosition = _ray.ToLocal(_player.GlobalPosition);
        _ray.ForceRaycastUpdate();
        Rid rid = _ray.GetColliderRid();
        if (rid != DependencyInjector.Ref().GetPlayer().GetRid()) {
            _cameraState.CollidingMaxDistance = _ray.GetCollisionPoint().DistanceTo(_player.GlobalPosition) - CollidingDistanceBuffer;
            _cameraState.CollidingMaxDistance = Mathf.Max(_cameraState.CollidingMaxDistance, 0f);
        }
        else {
            _cameraState.CollidingMaxDistance = float.MaxValue;
        }

        // smooth zoom
        if (!_zoomTimer.IsStopped() || _zoomDistanceTarget != null) {
            _cameraResetTimer.Start();
            if (_zoomDistanceTarget != null) {
                _zoom = _zoomDistanceTarget > _cameraState.Distance ? ZoomEnum.Out : ZoomEnum.In;
            }
            switch (_zoom) {
                case ZoomEnum.In:
                    _cameraState.Distance -= (float)delta * ZoomPerSecond;
                    break;
                case ZoomEnum.Out:
                    _cameraState.Distance += (float)delta * ZoomPerSecond;
                    break;
            }
            if (_zoomDistanceTarget != null && Mathf.Abs(_zoomDistanceTarget.Value - _cameraState.Distance) < (float)delta * ZoomPerSecond) {
                _zoomDistanceTarget = null;
            }
        }

        if (!DisableControls) {
            Vector2 controlDirection = Input.GetVector("rotate_camera_left", "rotate_camera_right", "rotate_camera_down", "rotate_camera_up");
            bool updated = controlDirection != Vector2.Zero;
            if (controlDirection.X != 0) {
                IsCameraDefault = false;
                _cameraState.Yaw -= (float)delta * _controllerRadiansPerSecond * controlDirection.X;
            }
            if (controlDirection.Y != 0) {
                _cameraState.Pitch += (float)delta * _controllerRadiansPerSecond * controlDirection.Y;
            }
            if (updated || _player.IsMoving() || !_zoomTimer.IsStopped()) {
                _cameraResetTimer.Start();
            }
            if (_cameraResetTimer.IsStopped()) {
                float diff = _cameraState.Yaw - _player.GlobalRotation.Y;
                if (Mathf.Abs(diff) % Mathf.Tau > (float)delta * ResetRadiansPerSecond) {
                    float sign = Mathf.Abs(diff) % Mathf.Tau > Mathf.Pi ? -Mathf.Sign(diff) : Mathf.Sign(diff);
                    _cameraState.Yaw += -sign * (float)delta * ResetRadiansPerSecond;
                }
                else {
                    IsCameraDefault = true;
                }
            }
            if (IsCameraDefault) {
                _cameraState.Yaw = Mathf.LerpAngle(_cameraState.Yaw, _player.GlobalRotation.Y, (float)delta * 0.9f);
            }
        }

        if (_area3D.HasOverlappingBodies()) {
            _cameraState = _lastCameraState;
            GD.Print("Colliding");
        }
        else {
            float _uncollidingDistance = Mathf.Min(_cameraState.Distance, _cameraState.CollidingMaxDistance);
            GlobalTransform = CameraTransform(_uncollidingDistance, _cameraState.Yaw, _cameraState.Pitch);
            _ray.GlobalTransform = CameraTransform(_cameraState.Distance, _cameraState.Yaw, _cameraState.Pitch);
            _lastCameraState = _cameraState;
        }
    }

    private Transform3D CameraTransform(float distance, float yaw, float pitch) {
        if (_player == null) {
            throw new System.Exception("Player is null");
        }

        Transform3D tf = new Transform3D(Basis.Identity, Vector3.Zero);

        tf = tf.Translated(-Vector3.Forward * distance);
        tf = tf.Rotated(Vector3.Left, pitch);
        tf = tf.Rotated(Vector3.Up, yaw);

        tf = tf.Translated(_player.GlobalTransform.Origin);

        return tf;
    }

    public void SetControlsDisabled(bool controlsDisabled) {
        DisableControls = controlsDisabled;
    }
}

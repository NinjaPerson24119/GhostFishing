using Godot;

public partial class FollowCamera : Camera3D {
    private enum ZoomEnum {
        In,
        Out,
    }
    [Export]
    public float Distance {
        get => _distance;
        set => _distance = Mathf.Clamp(value, MinDistance, MaxDistance);
    }
    private float _distance = 7f;
    [Export]
    public float MinDistance = 4f;
    [Export]
    public float MaxDistance = 15f;
    [Export]
    public float ZoomPerSecond = 20f;
    private ZoomEnum _zoom = ZoomEnum.In;
    Timer _zoomTimer = new Timer() {
        WaitTime = 0.1f,
        OneShot = true,
    };
    private int _zoomStep = 1;

    [Export]
    public float ControllerDegreesPerSecond {
        get => Mathf.RadToDeg(_controllerRadiansPerSecond);
        set => _controllerRadiansPerSecond = Mathf.DegToRad(value);
    }
    private float _controllerRadiansPerSecond = Mathf.DegToRad(120f);

    [Export]
    public float Yaw { get; private set; } = 0f;
    private float _followYaw = 0f;

    [Export]
    public float Pitch {
        get => _pitch;
        set => _pitch = Mathf.Clamp(value, MinPitch, MaxPitch);
    }
    private float _pitch = Mathf.DegToRad(30f);
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
            if (value != IsCameraDefault && _player != null) {
                Yaw = _followYaw;
            }
            _isCameraDefault = value;
        }
    }
    private bool _isCameraDefault = true;

    private Player? _player;
    private CharacterBody3D? _cameraBody;

    public FollowCamera() {
        Projection = ProjectionType.Perspective;
        Fov = 100f;
        Near = 0.1f;
        Far = 500f;

        float[] zoomSteps = GetZoomSteps();
        _distance = zoomSteps[_zoomStep];
    }

    public override void _Ready() {
        _player = DependencyInjector.Ref().GetPlayer();
        Yaw = _player.GlobalRotation.Y;

        _cameraBody = GetNode<CharacterBody3D>("CameraBody");

        AddChild(_cameraResetTimer);
        AddChild(_zoomTimer);
    }

    public override void _Input(InputEvent inputEvent) {
        if (DisableControls) {
            return;
        }

        if (inputEvent is InputEventMouseMotion mouseMotion) {
            Yaw -= mouseMotion.Relative.X * MouseSensitivity;
            Pitch += mouseMotion.Relative.Y * MouseSensitivity;
            IsCameraDefault = false;
            _cameraResetTimer.Start();
        }
        var mouseButtonEvent = inputEvent as InputEventMouseButton;
        if (mouseButtonEvent != null && mouseButtonEvent.Pressed) {
            switch (mouseButtonEvent.ButtonIndex) {
                case MouseButton.WheelUp:
                    _zoom = ZoomEnum.In;
                    _zoomTimer.Start();
                    break;
                case MouseButton.WheelDown:
                    _zoom = ZoomEnum.Out;
                    _zoomTimer.Start();
                    break;
            }
        }

        if (inputEvent.IsActionPressed("cycle_zoom")) {
            _zoomTimer.Stop();
            float[] zoomSteps = GetZoomSteps();
            _zoomStep = (_zoomStep + 1) % zoomSteps.Length;
            _distance = zoomSteps[_zoomStep];
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

    public override void _Process(double delta) {
        if (_player == null) {
            throw new System.Exception("Player is null");
        }
        if (_cameraBody == null) {
            throw new System.Exception("CameraBody is null");
        }

        if (!_zoomTimer.IsStopped()) {
            switch (_zoom) {
                case ZoomEnum.In:
                    Distance -= (float)delta * ZoomPerSecond;
                    break;
                case ZoomEnum.Out:
                    Distance += (float)delta * ZoomPerSecond;
                    break;
            }
        }

        // TODO: camera collides with terrain (avoid clipping)
        //_cameraBody.TestMove()
        //if (_cameraBody.)

        if (!DisableControls) {
            Vector2 controlDirection = Input.GetVector("rotate_camera_left", "rotate_camera_right", "rotate_camera_down", "rotate_camera_up");
            bool updated = controlDirection != Vector2.Zero;
            if (controlDirection.X != 0) {
                IsCameraDefault = false;
                Yaw -= (float)delta * _controllerRadiansPerSecond * controlDirection.X;
            }
            if (controlDirection.Y != 0) {
                Pitch += (float)delta * _controllerRadiansPerSecond * controlDirection.Y;
            }
            if (updated || _player.IsMoving() || !_zoomTimer.IsStopped()) {
                _cameraResetTimer.Start();
            }
            if (_cameraResetTimer.IsStopped()) {
                if (Mathf.Abs(Yaw - _player.GlobalRotation.Y) % Mathf.Tau > 0.01f) {
                    Yaw += -Mathf.Sign((Yaw - _player.GlobalRotation.Y) % Mathf.Tau) * (float)delta * ResetRadiansPerSecond;
                }
                else {
                    IsCameraDefault = true;
                }
            }
            _followYaw = Mathf.LerpAngle(_followYaw, _player.GlobalRotation.Y, (float)delta * 0.9f);
        }

        // TODO: raycast towards player to adjust distance

        Transform3D tf = new Transform3D(Basis.Identity, Vector3.Zero);
        tf = tf.Translated(-Vector3.Forward * Distance);
        tf = tf.Rotated(Vector3.Left, Pitch);

        float yaw = IsCameraDefault ? _followYaw : Yaw;
        tf = tf.Rotated(Vector3.Up, yaw);

        tf = tf.Translated(_player.GlobalTransform.Origin);
        GlobalTransform = tf;
    }

    public void SetControlsDisabled(bool controlsDisabled) {
        DisableControls = controlsDisabled;
    }
}

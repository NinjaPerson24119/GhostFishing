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
    private int _zoomStep = 0;

    [Export]
    public float ControllerDegreesPerSecond {
        get => Mathf.RadToDeg(_controllerRadiansPerSecond);
        set => _controllerRadiansPerSecond = Mathf.DegToRad(value);
    }
    private float _controllerRadiansPerSecond = Mathf.DegToRad(120f);

    [Export]
    public float Yaw { get; private set; } = 0f;

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

    private Player? _player;
    private CharacterBody3D? _cameraBody;
    private Timer _cameraResetTimer = new Timer() {
        WaitTime = 3f,
        OneShot = true,
    };

    public FollowCamera() {
        Projection = ProjectionType.Perspective;
        Fov = 100f;
        Near = 0.1f;
        Far = 500f;
    }

    public override void _Ready() {
        _player = DependencyInjector.Ref().GetPlayer();
        Yaw = _player.GlobalRotation.Y;

        _cameraBody = GetNode<CharacterBody3D>("CameraBody");

        AddChild(_cameraResetTimer);
        AddChild(_zoomTimer);
    }

    public override void _Input(InputEvent inputEvent) {
        if (inputEvent is InputEventMouseMotion mouseMotion) {
            Yaw -= mouseMotion.Relative.X * MouseSensitivity;
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

        bool updated = false;
        if (Input.IsActionPressed("rotate_camera_left")) {
            Yaw += (float)delta * _controllerRadiansPerSecond;
            updated = true;
        }
        else if (Input.IsActionPressed("rotate_camera_right")) {
            Yaw -= (float)delta * _controllerRadiansPerSecond;
            updated = true;
        }
        else if (Input.IsActionPressed("rotate_camera_up")) {
            Pitch += (float)delta * _controllerRadiansPerSecond;
            updated = true;
        }
        else if (Input.IsActionPressed("rotate_camera_down")) {
            Pitch -= (float)delta * _controllerRadiansPerSecond;
            updated = true;
        }
        if (updated || _player.IsMoving() || !_zoomTimer.IsStopped()) {
            _cameraResetTimer.Start();
        }
        if (_cameraResetTimer.IsStopped()) {
            if (Mathf.Abs(Yaw - _player.GlobalRotation.Y) > 0.01f) {
                Yaw += -Mathf.Sign(Yaw - _player.GlobalRotation.Y) * (float)delta * ResetRadiansPerSecond;
            }
        }

        // TODO: raycast towards player to adjust distance

        Transform3D tf = new Transform3D(Basis.Identity, Vector3.Zero);
        tf = tf.Translated(-Vector3.Forward * Distance);
        tf = tf.Rotated(Vector3.Left, Pitch);
        tf = tf.Rotated(Vector3.Up, Yaw);
        tf = tf.Translated(_player.GlobalTransform.Origin);
        GlobalTransform = tf;
    }
}

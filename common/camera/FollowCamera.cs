using Godot;

public partial class FollowCamera : Camera3D {
    [Export]
    public float Distance {
        get => _distance;
        set => _distance = Mathf.Clamp(value, MinDistance, MaxDistance);
    }
    private float _distance = 5f;
    [Export]
    public float MinDistance = 1f;
    [Export]
    public float MaxDistance = 15f;
    [Export]
    public float DistanceStep = 0.5f;

    [Export]
    public float Height = 2f;
    [Export]
    public float ControllerDegreesPerSecond {
        get => Mathf.RadToDeg(_controllerRadiansPerSecond);
        set => _controllerRadiansPerSecond = Mathf.DegToRad(value);
    }
    private float _controllerRadiansPerSecond = Mathf.DegToRad(120f);
    [Export]
    public float CameraPitchDegrees {
        get => Mathf.RadToDeg(_cameraPitchRadians);
        set => _cameraPitchRadians = Mathf.DegToRad(value);
    }
    private float _cameraPitchRadians = Mathf.DegToRad(-20f);

    [Export]
    public float MouseSensitivity = 0.005f;
    [Export]
    public float ResetRadiansPerSecond = Mathf.DegToRad(90f);

    private Player? _player;
    private CharacterBody3D? _cameraBody;
    private float _globalYaw;
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
        _globalYaw = _player.GlobalRotation.Y;

        _cameraBody = GetNode<CharacterBody3D>("CameraBody");

        AddChild(_cameraResetTimer);
    }

    public override void _Input(InputEvent inputEvent) {
        if (inputEvent is InputEventMouseMotion mouseMotion) {
            _globalYaw -= mouseMotion.Relative.X * MouseSensitivity;
            _cameraResetTimer.Start();
        }
        var mouseButtonEvent = inputEvent as InputEventMouseButton;
        if (mouseButtonEvent != null && mouseButtonEvent.Pressed) {
            switch (mouseButtonEvent.ButtonIndex) {
                case MouseButton.WheelUp:
                    Distance -= DistanceStep;
                    break;
                case MouseButton.WheelDown:
                    Distance += DistanceStep;
                    break;
            }
        }
    }

    public override void _Process(double delta) {
        if (_player == null) {
            throw new System.Exception("Player is null");
        }
        if (_cameraBody == null) {
            throw new System.Exception("CameraBody is null");
        }

        //_cameraBody.TestMove()
        //if (_cameraBody.)
        bool updated = false;
        if (Input.IsActionPressed("rotate_camera_left")) {
            _globalYaw += (float)delta * _controllerRadiansPerSecond;
            updated = true;
        }
        else if (Input.IsActionPressed("rotate_camera_right")) {
            _globalYaw -= (float)delta * _controllerRadiansPerSecond;
            updated = true;
        }
        if (updated || _player.IsMoving()) {
            _cameraResetTimer.Start();
        }
        if (_cameraResetTimer.IsStopped()) {
            if (Mathf.Abs(_globalYaw - _player.GlobalRotation.Y) > 0.01f) {
                _globalYaw += -Mathf.Sign(_globalYaw - _player.GlobalRotation.Y) * (float)delta * ResetRadiansPerSecond;
            }
        }

        // TODO: raycast towards player to adjust distance

        Transform3D tf = new Transform3D(Basis.Identity, Vector3.Zero);
        tf = tf.Translated(-Vector3.Forward * Distance);
        tf = tf.Rotated(Vector3.Up, _player.GlobalRotation.Y + _globalYaw);
        tf = tf.Rotated(Vector3.Right, Mathf.DegToRad(_cameraPitchRadians));
        tf = tf.Translated(_player.GlobalTransform.Origin);
        tf = tf.Translated(Vector3.Up * Height);
        //tf.
        GlobalTransform = tf;
    }
}

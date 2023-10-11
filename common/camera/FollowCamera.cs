using Godot;

public partial class FollowCamera : Camera3D {
    [Export]
    public float Distance = 5f;
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
    private float _yawOffset = 0f;
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
        _cameraBody = GetNode<CharacterBody3D>("CameraBody");
        AddChild(_cameraResetTimer);
    }

    public override void _Input(InputEvent inputEvent) {
        if (inputEvent is InputEventMouseMotion mouseMotion) {
            _yawOffset += mouseMotion.Relative.X * MouseSensitivity;
            _cameraResetTimer.Start();
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
        if (Input.IsActionPressed("rotate_camera_left")) {
            _yawOffset += (float)delta * _controllerRadiansPerSecond;
            _cameraResetTimer.Start();
        }
        else if (Input.IsActionPressed("rotate_camera_right")) {
            _yawOffset -= (float)delta * _controllerRadiansPerSecond;
            _cameraResetTimer.Start();
        }
        if (_cameraResetTimer.IsStopped()) {
            _yawOffset += -Mathf.Sign(_yawOffset) * (float)delta * ResetRadiansPerSecond;
            if (Mathf.Abs(_yawOffset) < 0.01f) {
                _yawOffset = 0f;
            }
        }

        // TODO: raycast towards player to adjust distance

        Transform3D tf = new Transform3D(Basis.Identity, Vector3.Zero);
        tf = tf.Translated(-Vector3.Forward * Distance);
        tf = tf.Rotated(Vector3.Up, _player.GlobalRotation.Y + _yawOffset);
        tf = tf.Rotated(Vector3.Right, Mathf.DegToRad(_cameraPitchRadians));
        tf = tf.Translated(_player.GlobalTransform.Origin);
        tf = tf.Translated(Vector3.Up * Height);
        //tf.
        GlobalTransform = tf;
    }
}

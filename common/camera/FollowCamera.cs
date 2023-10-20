using Godot;
using System.Linq;

public partial class FollowCamera : Node3D {
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

    public bool DisableControls {
        get {
            if (_playerContext != null) {
                return _playerContext.Controller.ControlsContext != ControlsContextType.Player;
            }
            // camera is always disabled if there is no player context
            return true;
        }
    }

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

    private PlayerContext? _playerContext;
    private Player? _player;

    public readonly float RayNearDistance = 1f;
    public readonly float RayPitchDistance = 3f;
    private Node3D? _rayCastGroup;
    private float _rayExtraDistance = 0.2f;
    private float _rayNearPitchPerSecond = Mathf.DegToRad(90f);
    private Timer _rayPitchUpTimer = new Timer() {
        WaitTime = 0.1f,
        OneShot = true,
    };

    public CameraState CameraState {
        get {
            return _cameraState;
        }
    }
    public void SetCameraState(CameraStateDTO dto) {
        _cameraState = new CameraState(this, dto);
        UpdateCameraTransform();
        _cameraResetTimer.Start();
    }
    private CameraState _cameraState;

    public FollowCamera() {
        float[] zoomSteps = GetZoomSteps();
        _cameraState = new CameraState(this, zoomSteps[_zoomStep]);
    }

    public override void _Ready() {
        _playerContext = DependencyInjector.Ref().GetLocalPlayerContext(GetPath());
        if (_playerContext == null) {
            throw new System.Exception("Player context is null. FollowCamera should be in the subtree of a PlayerContext");
        }
        _player = _playerContext.Player;
        _cameraState.Yaw = _player.GlobalRotation.Y;

        _rayCastGroup = GetNode<Node3D>("RayCastGroup");
        float radius = 1.0f;
        float backAdjust = 0.1f;
        _rayCastGroup.AddChild(new RayCast3D() {
            Position = Vector3.Zero,
            HitFromInside = true,
        });
        Vector3[] offsets = new Vector3[] {
            Vector3.Right + Vector3.Up,
            Vector3.Right + Vector3.Down,
            Vector3.Left + Vector3.Up,
            Vector3.Left + Vector3.Down,
            Vector3.Right,
            Vector3.Left,
            Vector3.Up,
            Vector3.Down,
        };
        foreach (Vector3 offset in offsets) {
            RayCast3D ray = new RayCast3D() {
                Position = offset * radius + Vector3.Back * backAdjust,
                HitFromInside = true,
                CollisionMask = 0,
            };
            // collide with terrain and player (so we can detect line-of-sight)
            ray.SetCollisionMaskValue(CollisionLayers.Player, true);
            ray.SetCollisionMaskValue(CollisionLayers.Terrain, true);
            _rayCastGroup.AddChild(ray);
        }

        AddChild(_cameraResetTimer);
        AddChild(_zoomTimer);
        AddChild(_rayPitchUpTimer);
    }

    public override void _Input(InputEvent inputEvent) {
        if (_playerContext == null) {
            throw new System.Exception("Player context is null");
        }
        if (DisableControls) {
            return;
        }

        if (_playerContext.Controller.MouseAllowed()) {
            HandleMouseInput(inputEvent);
        }

        if (inputEvent.IsActionPressed(_playerContext.ActionCycleZoom)) {
            _zoomTimer.Stop();
            float[] zoomSteps = GetZoomSteps();
            _zoomStep = (_zoomStep + 1) % zoomSteps.Length;
            _zoomDistanceTarget = zoomSteps[_zoomStep];
            _cameraResetTimer.Start();
        }
    }

    public void HandleMouseInput(InputEvent inputEvent) {
        if (inputEvent is InputEventMouseMotion mouseMotion) {
            _cameraState.Yaw -= mouseMotion.Relative.X * MouseSensitivity;
            if (mouseMotion.Relative.Y > 0 || !IsAutoPitchEnabled()) {
                _cameraState.Pitch += mouseMotion.Relative.Y * MouseSensitivity;
            }
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
        if (_rayCastGroup == null) {
            throw new System.Exception("Ray cast group is null");
        }
        if (_player == null) {
            throw new System.Exception("Player is null");
        }
        if (_playerContext == null) {
            throw new System.Exception("Player context is null");
        }

        _cameraState.CollidingMaxDistance = float.MaxValue;
        int maxRaycastIters = 2;
        int raycastIters = 0;
        while (raycastIters < maxRaycastIters) {
            float minCollidingDistance = float.MaxValue;
            var rayCastGroupChildren = _rayCastGroup.GetChildren();
            bool hitsThisIter = false;
            for (int i = 0; i < rayCastGroupChildren.Count; i++) {
                RayCast3D ray = (RayCast3D)rayCastGroupChildren[i];
                ray.TargetPosition = ray.ToLocal(_player.GlobalPosition);
                ray.ForceRaycastUpdate();
                Rid rid = ray.GetColliderRid();

                Rid[] playerRids = PlayerInjector.Ref().GetPlayers().Values.Select(p => p.GetRid()).ToArray();
                //GD.Print($"playerRids: {string.Join(", ", playerRids)}, hit rid: {rid}");
                if (playerRids.Contains(rid)) {
                    // we can't disable the collision mask for the player layer or we won't be able to detect line of sight
                    // so we just ignore it here
                    continue;
                }

                // not hitting any players
                minCollidingDistance = Mathf.Min(minCollidingDistance, ray.GetCollisionPoint().DistanceTo(_player.GlobalPosition));
                hitsThisIter = true;
            }
            if (minCollidingDistance < float.MaxValue && hitsThisIter) {
                _cameraState.CollidingMaxDistance = minCollidingDistance - CollidingDistanceBuffer;
                _cameraState.CollidingMaxDistance = Mathf.Max(_cameraState.CollidingMaxDistance, 0f);
            }
            else {
                // not hitting anything
                break;
            }
            UpdateRaycastGroupTransform(_cameraState.CollidingMaxDistance);
            raycastIters++;
        }
        if (_cameraState.CollidingMaxDistance < RayPitchDistance) {
            _rayPitchUpTimer.Start();
        }
        if (!_rayPitchUpTimer.IsStopped()) {
            _cameraState.Pitch += (float)delta * _rayNearPitchPerSecond;
            if (Mathf.Abs(_cameraState.CollidingMaxDistance - RayPitchDistance) < 0.01f) {
                _rayPitchUpTimer.Stop();
            }
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
            Vector2 controlDirection = _playerContext.CameraControlVector();
            bool updated = controlDirection != Vector2.Zero;
            if (controlDirection.X != 0) {
                IsCameraDefault = false;
                _cameraState.Yaw -= (float)delta * _controllerRadiansPerSecond * controlDirection.X;
            }
            if (controlDirection.Y != 0) {
                if (controlDirection.Y > 0 || !IsAutoPitchEnabled()) {
                    _cameraState.Pitch += (float)delta * _controllerRadiansPerSecond * controlDirection.Y;
                }
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
        else {
            // avoid immediate camera reset once controls are restored
            _cameraResetTimer.Start();
        }

        UpdateCameraTransform();
    }

    private void UpdateCameraTransform() {
        if (_rayCastGroup == null) {
            throw new System.Exception("Ray cast group is null");
        }

        float _uncollidingDistance = Mathf.Min(_cameraState.Distance, _cameraState.CollidingMaxDistance);
        GlobalTransform = CameraTransform(_uncollidingDistance, _cameraState.Yaw, _cameraState.Pitch);

        UpdateRaycastGroupTransform(_cameraState.Distance);
    }

    private void UpdateRaycastGroupTransform(float distance) {
        if (_rayCastGroup == null) {
            throw new System.Exception("Ray cast group is null");
        }
        _rayCastGroup.GlobalTransform = CameraTransform(distance + _rayExtraDistance, _cameraState.Yaw, _cameraState.Pitch);
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

    public bool IsAutoPitchEnabled() {
        return _cameraState.CollidingMaxDistance < RayPitchDistance || !_rayPitchUpTimer.IsStopped();
    }
}

using Godot;
using System;

public partial class Player : BuoyantBody, ITrackableObject {
    [Export]
    public PlayerID PlayerID {
        get {
            if (PlayerContext != null) {
                return PlayerContext.PlayerID;
            }
            if (_playerID == PlayerID.Invalid) {
                throw new Exception("Player must either define PlayerID or be a child of PlayerContext");
            }
            return _playerID;
        }
        set {
            _playerID = value;
        }
    }
    private PlayerID _playerID = PlayerID.Invalid;

    public PlayerContext? PlayerContext { get; private set; }

    public string TrackingID {
        get {
            return PlayerID.LongIDString();
        }
    }

    // F = ma, for a in m/s^2
    [Export]
    public float EngineForce = 9f;
    // rad/s^2
    [Export]
    public float TurnForce = 2.5f;

    [Export]
    public bool DisableControls = false;
    [Export]
    public float PositionChangedSignificanceEpsilon = Mathf.Pow(2f, 2);
    private Vector3 _lastSignificantPosition = Vector3.Zero;

    public Vector3 TrackingPosition {
        get {
            return GlobalPosition;
        }
    }

    private Ocean _ocean = null!;

    private float _horizontalSliceArea;
    private float _depthInWater = 1f;

    [Signal]
    public delegate void PositionChangedSignificantlyEventHandler(Vector3 position);

    public Player() {
        Size = new Vector3(2f, 3f, 2f);
        BuoyancyDamping = 0;
        ConstantLinearDrag = 1.5f;
        ConstantAngularDrag = 1f;
        AirDragCoefficient = 0.5f;
        AirMomentCoefficient = 0.08f;
        WaterDragCoefficient = 0.3f;
        WaterMomentCoefficient = 0.08f;
        SubmergedProportionOffset = 0.7f;
    }

    public override void _Ready() {
        PlayerContext = DependencyInjector.Ref().GetLocalPlayerContext(GetPath());
        if (PlayerContext != null) {
            GlobalPosition = PlayerContext.InitialGlobalPosition;
        }

        _ocean = DependencyInjector.Ref().GetOcean();

        WaterContactPointsNodePath = "Boat/WaterContactPoints";
        MeshInstance3D boundingBox = GetNode<MeshInstance3D>("BoundingBox");
        if (boundingBox != null && boundingBox.Mesh is BoxMesh) {
            BoxMesh boxMesh = (BoxMesh)boundingBox.Mesh;
            boxMesh.Size = Size;
        }

        base._Ready();
        AddChildInteractiveObject();
    }

    public override void _Process(double delta) {
        // update lagging Player position for the Ocean
        if (GlobalPosition.DistanceSquaredTo(_lastSignificantPosition) > PositionChangedSignificanceEpsilon) {
            EmitSignal(SignalName.PositionChangedSignificantly, GlobalPosition);
            _lastSignificantPosition = GlobalPosition;
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        ApplyForcesFromControls();
        base._IntegrateForces(state);
    }

    private void ApplyForcesFromControls() {
        if (PlayerContext == null) {
            return;
        }
        if (DisableControls) {
            return;
        }
        if (DepthInWater <= 0) {
            return;
        }

        var controlDirection = PlayerContext.MovementControlVector();
        Vector3 towardsFrontOfBoat = Vector3.Forward.Rotated(Vector3.Up, Rotation.Y);
        if (controlDirection.Y != 0) {
            ApplyCentralForce(towardsFrontOfBoat * EngineForce * Mass * controlDirection.Y);
        }
        if (controlDirection.X != 0) {
            ApplyTorque(Vector3.Down * TurnForce * Mass * controlDirection.X);
        }
    }

    public void ResetAboveWater(bool relocate = false, Vector2 globalXZ = default, float globalRotationY = 0f) {
        if (relocate) {
            CallDeferred(nameof(DeferredResetAboveWater), true, globalXZ, globalRotationY);
        }
        else {
            CallDeferred(nameof(DeferredResetAboveWater));
        }
    }

    // CallDeferred doesn't seem to understand default arguments, so we need to overload
    private void DeferredResetAboveWater() {
        DeferredResetAboveWater(false);
    }

    private void DeferredResetAboveWater(bool relocate = false, Vector2 globalXZ = default, float globalRotationY = 0f) {
        float yaw = GlobalRotation.Y;
        if (relocate) {
            yaw = globalRotationY;
        }

        Vector3 translation = new Vector3(GlobalPosition.X, _ocean.GlobalPosition.Y + 1f, GlobalPosition.Y);
        if (relocate) {
            translation.X = globalXZ.X;
            translation.Z = globalXZ.Y;
        }

        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;

        Transform3D transform = new Transform3D(Basis.Identity, Vector3.Zero);
        transform = transform.Rotated(Vector3.Up, yaw);
        transform = transform.Translated(translation);
        Transform = transform;
    }

    public void SetControlsDisabled(bool controlsDisabled) {
        DisableControls = controlsDisabled;
    }

    public bool IsMoving() {
        if (PlayerContext == null) {
            return false;
        }

        bool isMoving = false;
        foreach (string action in PlayerContext.MovementActions()) {
            if (Input.IsActionPressed(action)) {
                isMoving = true;
                break;
            }
        }
        return isMoving;
    }

    public void AddChildInteractiveObject() {
        // Player's inventory can only be accessed if
        // - Player cannot access its own inventory as an interactive object
        // - Player is not active
        InteractiveObject interactiveObject = new InteractiveObject() {
            TrackingID = $"PlayerBoatInventory-{PlayerID}",
            Description = "Open Boat Inventory",
        };
        PlayerStateView view = AssetManager.Ref().GetPlayerView(PlayerID);
        InteractiveObjectAction action = new OpenInventoryAction(view.BoatInventory.InventoryInstanceID);
        action.AddPrecondition(new NotPrecondition(new IsPlayerPrecondition(PlayerID)));
        action.AddPrecondition(new NotPrecondition(new PlayerActivePrecondition(PlayerID)));
        interactiveObject.AddAction(action);
        AddChild(interactiveObject);
    }
}

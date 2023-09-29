using Godot;

public partial class Player : RigidBody3D {
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float BuoyancyDamping = 0f;
    [Export(PropertyHint.Range, "0,1")]
    public float ConstantLinearDrag = 0.07f;
    [Export(PropertyHint.Range, "0,1")]
    public float ConstantAngularDrag = 0.05f;
    [Export(PropertyHint.Range, "0,1")]
    public float WaterLinearDrag = 0.03f;
    [Export(PropertyHint.Range, "0,1")]
    public float WaterAngularDrag = 0.01f;
    [Export]
    public float EngineAcceleration = 0.05f;
    [Export]
    public float TurnAcceleration = 0.03f;
    [Export]
    public float PositionChangedSignificanceEpsilon = Mathf.Pow(2f, 2);
    private Vector3 _lastSignificantPosition = Vector3.Zero;
    [Export]
    public bool DebugLogs = false;

    private float EngineForce => Mass * EngineAcceleration;
    private float TurnForce => Mass * TurnAcceleration;

    private Ocean _ocean;

    private float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
    private float _waterDensity = 1000; // kg/m^3
    private float _horizontalSliceArea;
    private float _depthInWater = 1f;
    private Vector3 _size = new Vector3(1f, 0.5f, 2.5f);

    [Signal]
    public delegate void PositionChangedSignificantlyEventHandler(Vector3 position);

    public override void _Ready() {
        _ocean = GetTree().Root.GetNode<Ocean>("/root/Main/Ocean");

        _horizontalSliceArea = _size.X * _size.Y;
        if (DebugLogs) {
            GD.Print($"Boat Size: {_size}. Horizontal slice area: {_horizontalSliceArea}");
        }

        (GetNode<MeshInstance3D>("BoundingBox").Mesh as BoxMesh).Size = _size;
    }

    public override void _Process(double delta) {
        // update lagging Player position for the Ocean
        if (GlobalPosition.DistanceSquaredTo(_lastSignificantPosition) > PositionChangedSignificanceEpsilon) {
            EmitSignal(SignalName.PositionChangedSignificantly, GlobalPosition);
            _lastSignificantPosition = GlobalPosition;
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        Transform = Transform.Orthonormalized();
        ApplyForcesFromControls();

        Node3D waterContactPoints = GetNode<Node3D>("Model/WaterContactPoints");
        float cumulativeDepth = 0;
        int submergedPoints = 0;
        var children = waterContactPoints.GetChildren();
        for (int i = 0; i < children.Count; i++) {
            Marker3D contactPoint = children[i] as Marker3D;
            // TODO: stop ignoring displacement on XZ plane
            // this should just call a GetHeight(), and the displacement should be internal to the Ocean
            Vector3 waterDisplacement = _ocean.GetDisplacement(new Vector2(contactPoint.GlobalPosition.X, contactPoint.GlobalPosition.Z));
            Vector3 waterContactPoint = new Vector3(contactPoint.GlobalPosition.X, _ocean.GlobalPosition.Y, contactPoint.GlobalPosition.Z) + waterDisplacement;

            // TODO: simplify for now by only considering Y
            if (DebugLogs) {
                GD.Print($"Water height ({waterContactPoint.Y}) = Water {contactPoint.GlobalPosition.Y} + Displacement {waterDisplacement.Y}, boat height = {contactPoint.GlobalPosition.Y}");
            }
            float depth = waterContactPoint.Y - contactPoint.GlobalPosition.Y;

            if (depth > 0) {
                submergedPoints++;
                // Archimedes Principle: F = ρgV
                float volumeDisplaced = _horizontalSliceArea * Mathf.Min(depth, _size.Y); ;
                if (DebugLogs) {
                    GD.Print($"Volume displaced: {volumeDisplaced}, depth: {depth}, horizontal slice area: {_horizontalSliceArea}, boat height: {_size.Y}");
                }
                float buoyancyForce = _waterDensity * _gravity * volumeDisplaced;
                ApplyForce(Vector3.Up * (1 - BuoyancyDamping) * buoyancyForce, contactPoint.GlobalPosition - GlobalPosition);
            }
        }
        if (submergedPoints > 0) {
            _depthInWater = cumulativeDepth / submergedPoints;
        }

        ApplyConstantDrag(state);
        ApplyWaterDrag(state);
    }

    private void ApplyForcesFromControls() {
        if (_depthInWater < 0) {
            return;
        }

        bool moveForward = Input.IsActionPressed("move_forward");
        bool moveBackward = Input.IsActionPressed("move_backward");
        Vector3 towardsFrontOfBoat = Vector3.Forward.Rotated(Vector3.Up, Rotation.Y);
        if (moveForward && !moveBackward) {
            ApplyCentralForce(towardsFrontOfBoat * EngineForce * Mass);
        }
        if (moveBackward && !moveForward) {
            ApplyCentralForce(towardsFrontOfBoat * -1 * EngineForce * Mass);
        }

        bool turnLeft = Input.IsActionPressed("turn_left");
        bool turnRight = Input.IsActionPressed("turn_right");
        if (turnLeft && !turnRight) {
            ApplyTorque(Vector3.Up * TurnForce * Mass);
        }
        if (turnRight && !turnLeft) {
            ApplyTorque(Vector3.Down * TurnForce * Mass);
        }
    }

    public void ApplyConstantDrag(PhysicsDirectBodyState3D state) {
        state.AngularVelocity *= 1 - ConstantAngularDrag;
        state.LinearVelocity *= 1 - ConstantLinearDrag;
    }

    public void ApplyWaterDrag(PhysicsDirectBodyState3D state) {
        if (_depthInWater <= 0) {
            return;
        }
        float submergedProportion = _depthInWater / _size.Y;
        DebugTools.Assert(submergedProportion >= 0 && submergedProportion <= 1, $"submergedProportion ({submergedProportion}) must be between 0 and 1");
        state.AngularVelocity *= 1 - WaterAngularDrag * submergedProportion;
        state.LinearVelocity *= 1 - WaterLinearDrag * submergedProportion;
    }

    public void ResetAboveWater() {
        CallDeferred(nameof(DeferredResetAboveWater));
    }

    public void DeferredResetAboveWater() {
        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;

        Transform3D transform = new Transform3D();
        // ignore the linter. you must set the basis and origin
        transform.Basis = Basis.Identity;
        transform.Origin = Vector3.Zero;
        transform = transform.Rotated(Vector3.Up, Rotation.Y);
        transform = transform.Translated(new Vector3(GlobalPosition.X, _ocean.GlobalPosition.Y + 1f, GlobalPosition.Z));
        Transform = transform;
    }
}

using Godot;

public partial class Player : RigidBody3D {
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float BuoyancyDamping = 0f;
    [Export]
    float WaterDrag = 0.07f;
    [Export]
    float WaterAngularDrag = 0.05f;
    [Export]
    float EngineForce = 30.0f;
    [Export]
    float TurnForce = 5.0f;

    [Export]
    public float PositionChangedSignificanceEpsilon = Mathf.Pow(2f, 2);
    private Vector3 _lastSignificantPosition = Vector3.Zero;

    private bool submerged = false;
    private float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
    private float _waterDensity = 1000; // kg/m^3
    private Aabb _absBounds;
    private float _horizontalSliceArea;
    private Ocean _ocean;

    [Signal]
    public delegate void PositionChangedSignificantlyEventHandler(Vector3 position);

    public override void _Ready() {
        _absBounds = GetNode<Node3D>("Model").GetNode<MeshInstance3D>("Boat").GetAabb().Abs();
        _horizontalSliceArea = _absBounds.Size.X * _absBounds.Size.Z;
        GD.Print($"Boat Size: {_absBounds.Size}. Horizontal slice area: {_horizontalSliceArea}");

        _ocean = GetTree().Root.GetNode<Ocean>("/root/Main/Ocean");
    }

    public override void _PhysicsProcess(double delta) {
        Node3D waterContactPoints = GetNode<Node3D>("WaterContactPoints");
        int submergedPoints = 0;
        int totalPoints = waterContactPoints.GetChildCount();
        foreach (Node3D contactPoint in waterContactPoints.GetChildren()) {
            // TODO: stop ignoring displacement on XZ plane
            // this should just call a GetHeight(), and the displacement should be internal to the Ocean
            Vector3 waterDisplacement = _ocean.GetDisplacement(new Vector2(contactPoint.GlobalPosition.X, contactPoint.GlobalPosition.Z));
            Vector3 waterContactPoint = new Vector3(contactPoint.GlobalPosition.X, _ocean.GlobalPosition.Y, contactPoint.GlobalPosition.Z) + waterDisplacement;

            // TODO: simplify for now by only considering Y
            //GD.Print($"Water height ({waterContactPoint.Y}) = Water {contactPoint.GlobalPosition.Y} + Displacement {waterDisplacement.Y}, boat height = {contactPoint.GlobalPosition.Y}");
            float depth = waterContactPoint.Y - contactPoint.GlobalPosition.Y;

            if (depth > 0) {
                submergedPoints++;
                // Archimedes Principle: F = ρgV
                float volumeDisplaced = EstimateVolumeDisplaced(depth);
                //GD.Print($"Volume displaced: {volumeDisplaced}, depth: {depth}, horizontal slice area: {_horizontalSliceArea}, boat height: {_absBounds.Size.Y}");
                float buoyancyForce = _waterDensity * _gravity * volumeDisplaced;
                ApplyForce(Vector3.Up * (1 - BuoyancyDamping) * buoyancyForce, contactPoint.GlobalPosition - GlobalPosition);
                break;
            }
        }
        submerged = submergedPoints > 0;
        PollControls();
    }

    public float EstimateVolumeDisplaced(float depth) {
        // TODO: this does not consider the shape of the boat
        // TODO: this does not consider the orientation of the boat

        // estimate as a rectangular prism
        return _horizontalSliceArea * Mathf.Min(depth, _absBounds.Size.Y);
    }

    public override void _Process(double delta) {
        // only notify when the player has moved significantly
        // primary subscriber to this is the Ocean for recentering
        if (GlobalPosition.DistanceSquaredTo(_lastSignificantPosition) > PositionChangedSignificanceEpsilon) {
            EmitSignal(SignalName.PositionChangedSignificantly, GlobalPosition);
            _lastSignificantPosition = GlobalPosition;
        }
    }

    private void PollControls() {
        if (submerged) {
            //GD.Print("Submerged");
            if (Input.IsActionPressed("move_forward")) {
                ApplyCentralForce(GlobalTransform.Basis.Z * EngineForce);
            }
            else if (Input.IsActionPressed("move_backward")) {
                ApplyCentralForce(GlobalTransform.Basis.Z * -1 * EngineForce);
            }
            if (Input.IsActionPressed("turn_left")) {
                ApplyTorque(Vector3.Up * TurnForce);
            }
            else if (Input.IsActionPressed("turn_right")) {
                ApplyTorque(Vector3.Down * TurnForce);
            }
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        if (submerged) {
            state.LinearVelocity *= 1 - WaterDrag;
            state.AngularVelocity *= 1 - WaterAngularDrag;
        }
    }

    public void ResetAboveWater() {
        CallDeferred(nameof(DeferredResetAboveWater));
    }

    public void DeferredResetAboveWater() {
        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;
        Ocean ocean = GetTree().Root.GetNode<Ocean>("/root/Main/Ocean");
        GlobalPosition = new Vector3(GlobalPosition.X, ocean.GlobalPosition.Y + 1f, GlobalPosition.Z);
        Rotation = Vector3.Zero;
    }
}

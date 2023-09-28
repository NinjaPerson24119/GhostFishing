using Godot;

public partial class Player : RigidBody3D {
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float BuoyancyDamping = 0f;
    [Export]
    public float LowerHalfVolumeReduction;
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
    private Marker3D _controlPoint;

    [Signal]
    public delegate void PositionChangedSignificantlyEventHandler(Vector3 position);

    public override void _Ready() {
        _absBounds = GetNode<Node3D>("Model").GetNode<MeshInstance3D>("Boat").GetAabb().Abs();
        _horizontalSliceArea = _absBounds.Size.X * _absBounds.Size.Z;
        GD.Print($"Boat Size: {_absBounds.Size}. Horizontal slice area: {_horizontalSliceArea}");

        _controlPoint = GetNode<Marker3D>("ControlPoint");
    }

    public override void _PhysicsProcess(double delta) {
        Node3D waterContactPoints = GetNode<Node3D>("WaterContactPoints");
        int submergedPoints = 0;
        int totalPoints = waterContactPoints.GetChildCount();
        foreach (Node3D contactPoint in waterContactPoints.GetChildren()) {
            // TODO: stop ignoring displacement on XZ plane
            Vector3 waterDisplacement = GetTree().Root.GetNode<Ocean>("/root/Main/Ocean").GetDisplacement(contactPoint.GlobalPosition);
            Vector3 waterContactPoint = contactPoint.GlobalPosition + waterDisplacement;
            GD.Print($"Water height = {waterContactPoint.Y}, boat height = {contactPoint.GlobalPosition.Y}");
            float depth = 0 - contactPoint.GlobalPosition.Y;
            if (depth > 0) {
                submergedPoints++;
                // Archimedes Principle: F = ρgV
                float volumeDisplaced = _horizontalSliceArea * Mathf.Min(depth, _absBounds.Size.Y);
                GD.Print($"Volume displaced: {volumeDisplaced}, depth: {depth}, horizontal slice area: {_horizontalSliceArea}, boat height: {_absBounds.Size.Y}");
                float buoyancyForce = _waterDensity * _gravity * volumeDisplaced;
                ApplyForce(Vector3.Up * (1 - BuoyancyDamping) * (float)delta * distributedForce, contactPoint.GlobalPosition - GlobalPosition);
                break;
            }
        }
        submerged = submergedPoints > 0;
        PollControls();
    }

    public void EstimateVolumeDisplaced(float depth) {
        // clip the depth to the height of the boat
        depth = Mathf.Min(depth, _absBounds.Size.Y);

        float halfHeight = _absBounds.Size.Y / 2;

        // estimate lower half of boat with a percentage reduce because of hull curvature
        float lowerHalfHeight = Mathf.Min(depth, halfHeight);
        float lowerHalfVolume = _horizontalSliceArea * lowerHalfHeight * _lowerHalfVolumeReduction;

        float upperHalfHeight = Mathf.Max(depth - halfHeight, 0);
        float volumeDisplaced = _horizontalSliceArea * Mathf.Min(height, _absBounds.Size.Y);
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
            GD.Print("Submerged");
            Vector3 towardsFront = (GlobalPosition - _controlPoint.GlobalPosition).Normalized();
            if (Input.IsActionPressed("move_forward")) {
                ApplyForce(towardsFront * EngineForce, _controlPoint.GlobalPosition);
            }
            else if (Input.IsActionPressed("move_backward")) {
                ApplyCentralForce(towardsFront * -1 * EngineForce);
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

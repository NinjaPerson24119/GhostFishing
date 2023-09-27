using Godot;

public partial class Player : RigidBody3D {
    [Export]
    public float FloatForce = 2.0f;
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
        foreach (Node3D contactPoint in waterContactPoints.GetChildren()) {
            // TODO: forgot to compute Gerstner heights
            float waterY = GetTree().Root.GetNode<Ocean>("/root/Main/Ocean").GetHeight(contactPoint.GlobalPosition);
            float depth = waterY - contactPoint.GlobalPosition.Y;
            if (depth > 0) {
                submergedPoints++;
                // Archimedes Principle: F = ρgV
                float volumeDisplaced = _horizontalSliceArea * depth;
                GD.Print($"Old: {_gravity * FloatForce * depth}");
                GD.Print($"New: {_waterDensity * _gravity * volumeDisplaced}, depth: {depth}, delta: {delta}");
                ApplyForce(Vector3.Up * (float)delta * _waterDensity * _gravity * volumeDisplaced, contactPoint.GlobalPosition);
            }
        }
        submerged = submergedPoints > 0;
        PollControls();
    }

    public override void _Process(double delta) {
        // only notify when the player has moved significantly
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
}

using Godot;

public partial class Player : RigidBody3D {
    [Export(PropertyHint.Range, "0,10")]
    public float DepthInWater = 2.0f;
    [Export(PropertyHint.Range, "0,1")]
    float WaterDrag = 0.07f;
    [Export(PropertyHint.Range, "0,1")]
    float WaterAngularDrag = 0.05f;
    [Export]
    float EngineForce = 30.0f;
    [Export]
    float TurnForce = 5.0f;

    [Export]
    public float PositionChangedSignificanceEpsilon = Mathf.Pow(2f, 2);
    private Vector3 _lastSignificantPosition = Vector3.Zero;

    private Aabb _absBounds;
    private Ocean _ocean;

    [Signal]
    public delegate void PositionChangedSignificantlyEventHandler(Vector3 position);

    public override void _Ready() {
        _absBounds = GetNode<Node3D>("Visual/Model").GetNode<MeshInstance3D>("Boat").GetAabb().Abs();
        _ocean = GetTree().Root.GetNode<Ocean>("/root/Main/Ocean");
    }

    public override void _Process(double delta) {
        // update lagging Player position for the Ocean
        if (GlobalPosition.DistanceSquaredTo(_lastSignificantPosition) > PositionChangedSignificanceEpsilon) {
            EmitSignal(SignalName.PositionChangedSignificantly, GlobalPosition);
            _lastSignificantPosition = GlobalPosition;
        }
    }

    public override void _PhysicsProcess(double delta) {
        ApplyMovement(delta);
    }

    private void ApplyMovement(double delta) {
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

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        // we need to zero out the Y component of the velocity because we don't physics to move the boat up and down
        float waterDragFactor = 1 - WaterDrag;
        state.LinearVelocity = new Vector3(state.LinearVelocity.X * waterDragFactor, 0, state.LinearVelocity.Z * waterDragFactor);

        state.AngularVelocity *= 1 - WaterAngularDrag;

        // keep the boat at the surface of the water
        float boatY = _ocean.GlobalPosition.Y + _absBounds.Size.Y / 2 - DepthInWater;
        GlobalPosition = new Vector3(GlobalPosition.X, boatY, GlobalPosition.Z);
    }
}

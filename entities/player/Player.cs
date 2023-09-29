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
        bool moveForward = Input.IsActionPressed("move_forward");
        bool moveBackward = Input.IsActionPressed("move_backward");
        if (moveForward && !moveBackward) {
            ApplyCentralForce(GlobalTransform.Basis.Z * EngineForce);
        }
        if (moveBackward && !moveForward) {
            ApplyCentralForce(GlobalTransform.Basis.Z * -1 * EngineForce);
        }

        bool turnLeft = Input.IsActionPressed("turn_left");
        bool turnRight = Input.IsActionPressed("turn_right");
        if (turnLeft && !turnRight) {
            ApplyTorque(Vector3.Up * TurnForce);
        }
        if (turnRight && !turnLeft) {
            ApplyTorque(Vector3.Down * TurnForce);
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        state.LinearVelocity *= 1 - WaterDrag;
        state.AngularVelocity *= 1 - WaterAngularDrag;

        // keep the boat at the surface of the water
        float boatY = _ocean.GlobalPosition.Y + _absBounds.Size.Y / 2 - DepthInWater;
        GlobalPosition = new Vector3(GlobalPosition.X, boatY, GlobalPosition.Z);
    }
}

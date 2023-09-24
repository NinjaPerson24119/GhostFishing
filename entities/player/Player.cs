using Godot;

public partial class Player : RigidBody3D {
    float gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
    float float_force = 2.0f;
    float water_drag = 0.05f;
    float water_angular_drag = 0.05f;
    bool submerged = false;
    float engine_force = 30.0f;
    float turn_force = 5.0f;

    [Export]
    public float PositionChangedSignificanceEpsilon = Mathf.Pow(2f, 2);
    private Vector3 _lastSignificantPosition = Vector3.Zero;

    [Signal]
    public delegate void PositionChangedSignificantlyEventHandler(Vector3 position);

    public override void _Ready() {
        SetNotifyTransform(true);
    }

    public override void _PhysicsProcess(double delta) {
        var depth = GetTree().Root.GetNode<Ocean>("/root/Main/Ocean").GetHeight(GlobalPosition) - GlobalPosition.Y;
        submerged = depth > 0;
        if (submerged) {
            // TODO: vary forces by Archimedes principle
            ApplyCentralForce(Vector3.Up * gravity * float_force * depth);
        }
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
            if (Input.IsActionPressed("move_forward")) {
                ApplyCentralForce(Basis.Z * engine_force);
            }
            else if (Input.IsActionPressed("move_backward")) {
                ApplyCentralForce(Basis.Z * -1 * engine_force);
            }
            if (Input.IsActionPressed("turn_left")) {
                ApplyTorque(Vector3.Up * turn_force);
            }
            else if (Input.IsActionPressed("turn_right")) {
                ApplyTorque(Vector3.Down * turn_force);
            }
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        if (submerged) {
            state.LinearVelocity *= 1 - water_drag;
            state.AngularVelocity *= 1 - water_angular_drag;
        }
    }
}

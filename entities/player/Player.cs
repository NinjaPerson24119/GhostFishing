using Godot;

public partial class Player : CharacterBody3D {
    [Export(PropertyHint.Range, "0,10")]
    public float BoatDepth = 2.0f;
    [Export(PropertyHint.Range, "0,100")]
    public float EngineMetersPerSecond = 50f;
    [Export(PropertyHint.Range, "0,360")]
    public float TurnDegreesPerSecond = 30;
    private float _turnRadiansPerSecond => Mathf.DegToRad(TurnDegreesPerSecond);

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
        _ocean = GetTree().Root.GetNode<Ocean>("/root/Main/Ocean");
    }

    public override void _Process(double delta) {
        // only notify when the player has moved significantly
        // primary subscriber to this is the Ocean for recentering
        if (GlobalPosition.DistanceSquaredTo(_lastSignificantPosition) > PositionChangedSignificanceEpsilon) {
            EmitSignal(SignalName.PositionChangedSignificantly, GlobalPosition);
            _lastSignificantPosition = GlobalPosition;
        }
    }

    public override void _PhysicsProcess(double delta) {
        ApplyMovement(delta);
    }

    private void ApplyMovement(double delta) {
        float yawChange = 0;
        bool turnLeft = Input.IsActionPressed("turn_left");
        bool turnRight = Input.IsActionPressed("turn_right");
        if (turnLeft && !turnRight) {
            yawChange = _turnRadiansPerSecond;
        }
        if (turnRight && !turnLeft) {
            yawChange = -_turnRadiansPerSecond;
        }
        GlobalRotation = new Vector3(GlobalRotation.X, GlobalRotation.Y + yawChange * (float)delta, GlobalRotation.Z);

        Vector3 targetVelocity = Vector3.Zero;
        if (Input.IsActionPressed("move_forward")) {
            targetVelocity += GlobalTransform.Basis.Z * EngineMetersPerSecond;
        }
        else if (Input.IsActionPressed("move_backward")) {
            targetVelocity += GlobalTransform.Basis.Z * EngineMetersPerSecond;
        }
        //targetVelocity.Y = 0;
        Velocity = targetVelocity * (float)delta;
        MoveAndSlide();

        Vector3 waterDisplacement = _ocean.GetDisplacement(new Vector2(GlobalPosition.X, GlobalPosition.Z));
        float boatY = _ocean.GlobalPosition.Y + waterDisplacement.Y + _absBounds.Size.Y / 2 - BoatDepth;
        GlobalPosition = new Vector3(GlobalPosition.X, boatY, GlobalPosition.Z);
    }
}

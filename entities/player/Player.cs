using Godot;

public partial class Player : RigidBody3D {
    [Export(PropertyHint.Range, "0,10")]
    public float BoatDepth = 2.0f;

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

    private Aabb _absBounds;
    private Ocean _ocean;

    [Signal]
    public delegate void PositionChangedSignificantlyEventHandler(Vector3 position);

    public override void _Ready() {
        _absBounds = GetNode<Node3D>("Model").GetNode<MeshInstance3D>("Boat").GetAabb().Abs();
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
        state.LinearVelocity *= 1 - WaterDrag;
        state.AngularVelocity *= 1 - WaterAngularDrag;

        //Vector3 waterPositionDisplacement = _ocean.GetDisplacement(new Vector2(GlobalPosition.X, GlobalPosition.Z));
        //float boatY = _ocean.GlobalPosition.Y + waterPositionDisplacement.Y + _absBounds.Size.Y / 2 - BoatDepth;
        //GlobalPosition = new Vector3(GlobalPosition.X, boatY, GlobalPosition.Z);

        ComputeBoatRotation();
    }

    public Vector2 ComputeBoatRotation() {
        // get the points around the boat AABB in cross shape (+)
        float halfWidth = _absBounds.Size.X / 2;
        float halfLength = _absBounds.Size.Z / 2;
        Vector3 front = new Vector3(GlobalPosition.X, 0, GlobalPosition.Z + halfLength).Rotated(Vector3.Up, GlobalRotation.Y);
        Vector3 back = new Vector3(GlobalPosition.X, 0, GlobalPosition.Z - halfLength).Rotated(Vector3.Up, GlobalRotation.Y);
        Vector3 left = new Vector3(GlobalPosition.X - halfWidth, 0, GlobalPosition.Z).Rotated(Vector3.Up, GlobalRotation.Y);
        Vector3 right = new Vector3(GlobalPosition.X + halfWidth, 0, GlobalPosition.Z).Rotated(Vector3.Up, GlobalRotation.Y);
        GetNode<MeshInstance3D>("WaterApproximation/FrontSphere").GlobalPosition = front;
        GetNode<MeshInstance3D>("WaterApproximation/BackSphere").GlobalPosition = back;
        GetNode<MeshInstance3D>("WaterApproximation/LeftSphere").GlobalPosition = left;
        GetNode<MeshInstance3D>("WaterApproximation/RightSphere").GlobalPosition = right;

        // adjust the points to the ocean surface
        Vector3[] pointsToDisplace = { front, back, left, right };
        for (int i = 0; i < pointsToDisplace.Length; i++) {
            Vector3 displacement = _ocean.GetDisplacement(new Vector2(pointsToDisplace[i].X, pointsToDisplace[i].Z));
            pointsToDisplace[i].Y += displacement.Y;
        }
        GetNode<MeshInstance3D>("WaterApproximation/FrontSphere").GlobalPosition = pointsToDisplace[0];
        GetNode<MeshInstance3D>("WaterApproximation/BackSphere").GlobalPosition = pointsToDisplace[1];
        GetNode<MeshInstance3D>("WaterApproximation/LeftSphere").GlobalPosition = pointsToDisplace[2];
        GetNode<MeshInstance3D>("WaterApproximation/RightSphere").GlobalPosition = pointsToDisplace[3];

        // compute the rotation
        Vector3 frontToBack = back - front;
        Vector3 leftToRight = right - left;
        Vector3 normal = frontToBack.Cross(leftToRight).Normalized();
        float pitch = Mathf.Acos(normal.Dot(Vector3.Forward));
        float roll = Mathf.Acos(normal.Dot(Vector3.Right));

        GD.Print($"pitch: {Mathf.RadToDeg(pitch)}, roll: {Mathf.RadToDeg(roll)}");
        //GetNode<MeshInstance3D>("WaterApproximation/WaterPlane").GlobalRotation = new Vector3(pitch, GlobalRotation.Y, roll);

        return Vector2.Zero;
    }
}

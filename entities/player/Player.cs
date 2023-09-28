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
        // we need to zero out the Y component of the velocity because we don't physics to move the boat up and down
        float waterDragFactor = 1 - WaterDrag;
        state.LinearVelocity = new Vector3(state.LinearVelocity.X * waterDragFactor, 0, state.LinearVelocity.Z * waterDragFactor);

        state.AngularVelocity *= 1 - WaterAngularDrag;

        //Vector3 waterPositionDisplacement = _ocean.GetDisplacement(new Vector2(GlobalPosition.X, GlobalPosition.Z));
        //float boatY = _ocean.GlobalPosition.Y + waterPositionDisplacement.Y + _absBounds.Size.Y / 2 - BoatDepth;
        //GlobalPosition = new Vector3(GlobalPosition.X, boatY, GlobalPosition.Z);

        var angles = EstimatePitchRollWithWaves();
        GlobalRotation = new Vector3(angles.pitch, GlobalRotation.Y, angles.roll);
    }

    public (float pitch, float roll) EstimatePitchRollWithWaves() {
        // get the points around the boat AABB in cross shape (+)
        float halfWidth = _absBounds.Size.X / 2;
        float halfLength = _absBounds.Size.Z / 2;
        Vector3 GlobalPositionXZ = new Vector3(GlobalPosition.X, 0, GlobalPosition.Z);
        Vector3 front = new Vector3(0, 0, halfLength).Rotated(Vector3.Up, GlobalRotation.Y) + GlobalPositionXZ;
        Vector3 back = new Vector3(0, 0, -halfLength).Rotated(Vector3.Up, GlobalRotation.Y) + GlobalPositionXZ;
        Vector3 left = new Vector3(-halfWidth, 0, 0).Rotated(Vector3.Up, GlobalRotation.Y) + GlobalPositionXZ;
        Vector3 right = new Vector3(halfWidth, 0, 0).Rotated(Vector3.Up, GlobalRotation.Y) + GlobalPositionXZ;

        // adjust the points to the ocean surface
        float averageDisplacementY = 0;
        Vector3[] pointsToDisplace = { front, back, left, right };
        for (int i = 0; i < pointsToDisplace.Length; i++) {
            Vector3 displacement = _ocean.GetDisplacement(new Vector2(pointsToDisplace[i].X, pointsToDisplace[i].Z));
            pointsToDisplace[i].Y += displacement.Y;
            averageDisplacementY += displacement.Y;
        }
        averageDisplacementY /= pointsToDisplace.Length;
        front = pointsToDisplace[0];
        back = pointsToDisplace[1];
        left = pointsToDisplace[2];
        right = pointsToDisplace[3];

        GetNode<MeshInstance3D>("WaterApproximation/FrontSphere").GlobalPosition = front;
        GetNode<MeshInstance3D>("WaterApproximation/BackSphere").GlobalPosition = back;
        GetNode<MeshInstance3D>("WaterApproximation/LeftSphere").GlobalPosition = left;
        GetNode<MeshInstance3D>("WaterApproximation/RightSphere").GlobalPosition = right;

        // compute the rotation
        //Vector3 normal = frontToBack.Cross(leftToRight).Normalized();
        float angleFront = Mathf.Acos(front.Normalized().Dot(GlobalTransform.Basis.Z));
        float angleBack = Mathf.Acos(back.Normalized().Dot(-GlobalTransform.Basis.Z));
        float angleLeft = Mathf.Acos(left.Normalized().Dot(-GlobalTransform.Basis.X));
        float angleRight = Mathf.Acos(right.Normalized().Dot(GlobalTransform.Basis.X));

        // assign outputs
        float pitch = (angleFront + angleBack) / 2;
        float roll = (angleLeft + angleRight) / 2;

        GD.Print($"pitch: {Mathf.RadToDeg(pitch)}, roll: {Mathf.RadToDeg(roll)}");
        var waterPlane = GetNode<MeshInstance3D>("WaterApproximation/WaterPlane");

        waterPlane.Position = new Vector3(0, averageDisplacementY, 0);
        waterPlane.Rotation = new Vector3(pitch, GlobalRotation.Y, roll);
        // BUG: why does changing the rotation reset the scale?
        waterPlane.Scale = new Vector3(2.5f, 1, 4.5f);

        return (pitch, roll);
    }
}

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

    const int averageWindowSize = 100;
    private MovingAverage _pitchAverage = new MovingAverage(averageWindowSize);
    private MovingAverage _rollAverage = new MovingAverage(averageWindowSize);
    private MovingAverage _displacementYAverage = new MovingAverage(averageWindowSize);

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

        var aug = EstimateWaterAugmentations();
        _pitchAverage.AddValue(aug.pitch);
        _rollAverage.AddValue(aug.roll);
        _displacementYAverage.AddValue(aug.displacementY);

        float pitch = (float)_pitchAverage.GetValue();
        float roll = (float)_rollAverage.GetValue();
        float displacementY = (float)_displacementYAverage.GetValue();
        GD.Print($"pitch: {Mathf.RadToDeg(pitch)}, roll: {Mathf.RadToDeg(roll)}, displacementY: {displacementY}");

        GlobalRotation = new Vector3(pitch, GlobalRotation.Y, roll);
        GlobalPosition = new Vector3(GlobalPosition.X, displacementY, GlobalPosition.Z);
    }

    private void ApplyMovement(double delta) {
        bool moveForward = Input.IsActionPressed("move_forward");
        bool moveBackward = Input.IsActionPressed("move_backward");
        Vector3 towardsFrontOfBoat = Vector3.Forward.Rotated(Vector3.Up, Rotation.Y);
        if (moveForward && !moveBackward) {
            ApplyCentralForce(towardsFrontOfBoat * EngineForce);
        }
        if (moveBackward && !moveForward) {
            ApplyCentralForce(towardsFrontOfBoat * -1 * EngineForce);
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

    // estimates augmentations to the transform, based on the ocean waves
    public (float pitch, float roll, float displacementY) EstimateWaterAugmentations() {
        // get the points around the boat AABB in cross shape (+)
        float halfWidth = _absBounds.Size.X / 2;
        float halfLength = _absBounds.Size.Z / 2;
        Vector3 front = new Vector3(0, 0, -halfLength);
        Vector3 back = new Vector3(0, 0, halfLength);
        Vector3 left = new Vector3(-halfWidth, 0, 0);
        Vector3 right = new Vector3(halfWidth, 0, 0);

        // adjust the points to the ocean surface
        float averageDisplacementY = 0;
        Vector3 GlobalPositionXZ = new Vector3(GlobalPosition.X, 0, GlobalPosition.Z);
        Vector3[] pointsToDisplace = { front, back, left, right };
        for (int i = 0; i < pointsToDisplace.Length; i++) {
            Vector3 globalPositionToDisplace = pointsToDisplace[i].Rotated(Vector3.Up, GlobalRotation.Y) + GlobalPositionXZ;
            Vector3 displacement = _ocean.GetDisplacement(new Vector2(globalPositionToDisplace.X, globalPositionToDisplace.Z));
            GD.Print($"displacement: {displacement}");
            pointsToDisplace[i].Y += displacement.Y;
            averageDisplacementY += displacement.Y;
        }
        averageDisplacementY /= pointsToDisplace.Length;
        front = pointsToDisplace[0];
        back = pointsToDisplace[1];
        left = pointsToDisplace[2];
        right = pointsToDisplace[3];

        // compute angles to the waves
        float angleFront = Mathf.Acos(front.Normalized().Dot(Vector3.Forward));
        float angleBack = Mathf.Acos(back.Normalized().Dot(Vector3.Back));
        float angleLeft = Mathf.Acos(left.Normalized().Dot(Vector3.Left));
        float angleRight = Mathf.Acos(right.Normalized().Dot(Vector3.Right));
        //GD.Print($"angleFront: {Mathf.RadToDeg(angleFront)}, angleBack: {Mathf.RadToDeg(angleBack)}, angleLeft: {Mathf.RadToDeg(angleLeft)}, angleRight: {Mathf.RadToDeg(angleRight)}");

        float pitch = (angleFront + angleBack) / 2;
        float roll = (angleLeft + angleRight) / 2;


        //GD.Print($"pitch: {Mathf.RadToDeg(pitch)}, roll: {Mathf.RadToDeg(roll)}");
        GetNode<MeshInstance3D>("WaterApproximation/FrontSphere").GlobalPosition = front.Rotated(Vector3.Up, GlobalRotation.Y) + GlobalPositionXZ;
        GetNode<MeshInstance3D>("WaterApproximation/BackSphere").GlobalPosition = back.Rotated(Vector3.Up, GlobalRotation.Y) + GlobalPositionXZ;
        GetNode<MeshInstance3D>("WaterApproximation/LeftSphere").GlobalPosition = left.Rotated(Vector3.Up, GlobalRotation.Y) + GlobalPositionXZ;
        GetNode<MeshInstance3D>("WaterApproximation/RightSphere").GlobalPosition = right.Rotated(Vector3.Up, GlobalRotation.Y) + GlobalPositionXZ;

        return (pitch, roll, averageDisplacementY);
    }
}

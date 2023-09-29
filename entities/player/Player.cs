using Godot;

public partial class Player : RigidBody3D {
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float BuoyancyDamping = 0f;
    [Export(PropertyHint.Range, "0,1")]
    float WaterDrag = 0.07f;
    [Export(PropertyHint.Range, "0,1")]
    float WaterAngularDrag = 0.05f;
    [Export]
    float EngineAcceleration = 0.05f;
    [Export]
    float TurnAcceleration = 0.03f;

    [Export]
    public float PositionChangedSignificanceEpsilon = Mathf.Pow(2f, 2);
    private Vector3 _lastSignificantPosition = Vector3.Zero;

    private float EngineForce => Mass * EngineAcceleration;
    private float TurnForce => Mass * TurnAcceleration;

    private Ocean _ocean;

    private float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
    private float _waterDensity = 1000; // kg/m^3
    private float _horizontalSliceArea;
    private float _depthInWater = 1f;
    private Vector3 _size = new Vector3(2.5f, 0.5f, 1f);

    const int averageWindowSize = 100;
    private MovingAverage _pitchAverage = new MovingAverage(averageWindowSize);
    private MovingAverage _rollAverage = new MovingAverage(averageWindowSize);
    private MovingAverage _displacementYAverage = new MovingAverage(averageWindowSize);

    [Signal]
    public delegate void PositionChangedSignificantlyEventHandler(Vector3 position);

    public override void _Ready() {
        _ocean = GetTree().Root.GetNode<Ocean>("/root/Main/Ocean");

        _horizontalSliceArea = _size.X * _size.Y;
        GD.Print($"Boat Size: {_size}. Horizontal slice area: {_horizontalSliceArea}");

        (GetNode<MeshInstance3D>("BoundingBox").Mesh as BoxMesh).Size = _size;
    }

    public override void _Process(double delta) {
        // update lagging Player position for the Ocean
        if (GlobalPosition.DistanceSquaredTo(_lastSignificantPosition) > PositionChangedSignificanceEpsilon) {
            EmitSignal(SignalName.PositionChangedSignificantly, GlobalPosition);
            _lastSignificantPosition = GlobalPosition;
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        ApplyForcesFromControls();

        /*
        Vector3 displacement = _ocean.GetDisplacement(new Vector2(GlobalPosition.X, GlobalPosition.Z));
        float waterHeight = _ocean.GlobalPosition.Y + displacement.Y;
        _depthInWater = waterHeight - GlobalPosition.Y + _size.Y / 2;

        if (_depthInWater > 0) {
            float volumeDisplaced = _horizontalSliceArea * Mathf.Min(_depthInWater, _size.Y);
            GD.Print($"Volume displaced: {volumeDisplaced}, depth: {_depthInWater}, horizontal slice area: {_horizontalSliceArea}, boat height: {_size.Y}");

            // Archimedes' principle
            float buoyancyForce = _waterDensity * _gravity * volumeDisplaced * (1 - BuoyancyDamping);
            GD.Print($"Buoyancy force: {_waterDensity * _gravity * volumeDisplaced}");
            ApplyCentralForce(Vector3.Up * buoyancyForce);
        }
        */

        Node3D waterContactPoints = GetNode<Node3D>("WaterContactPoints");
        float cumulativeDepth = 0;
        int submergedPoints = 0;
        foreach (Marker3D contactPoint in waterContactPoints.GetChildren()) {
            // TODO: stop ignoring displacement on XZ plane
            // this should just call a GetHeight(), and the displacement should be internal to the Ocean
            Vector3 waterDisplacement = _ocean.GetDisplacement(new Vector2(contactPoint.GlobalPosition.X, contactPoint.GlobalPosition.Z));
            Vector3 waterContactPoint = new Vector3(contactPoint.GlobalPosition.X, _ocean.GlobalPosition.Y, contactPoint.GlobalPosition.Z) + waterDisplacement;

            // TODO: simplify for now by only considering Y
            GD.Print($"Water height ({waterContactPoint.Y}) = Water {contactPoint.GlobalPosition.Y} + Displacement {waterDisplacement.Y}, boat height = {contactPoint.GlobalPosition.Y}");
            float depth = waterContactPoint.Y - contactPoint.GlobalPosition.Y;

            if (depth > 0) {
                submergedPoints++;
                // Archimedes Principle: F = ρgV
                float volumeDisplaced = _horizontalSliceArea * Mathf.Min(depth, _size.Y); ;
                //GD.Print($"Volume displaced: {volumeDisplaced}, depth: {depth}, horizontal slice area: {_horizontalSliceArea}, boat height: {_absBounds.Size.Y}");
                float buoyancyForce = _waterDensity * _gravity * volumeDisplaced;
                //ApplyForce(Vector3.Up * (1 - BuoyancyDamping) * buoyancyForce, contactPoint.GlobalPosition - GlobalPosition);
            }
        }
        _depthInWater = cumulativeDepth / submergedPoints;

        ApplyWaterDrag(state);

        var aug = EstimateWaterAugmentations();
        _pitchAverage.AddValue(aug.pitch);
        _rollAverage.AddValue(aug.roll);
        _displacementYAverage.AddValue(aug.displacementY);

        float pitch = (float)_pitchAverage.GetValue();
        float roll = (float)_rollAverage.GetValue();
        //float displacementY = (float)_displacementYAverage.GetValue();
        //GD.Print($"pitch: {Mathf.RadToDeg(pitch)}, roll: {Mathf.RadToDeg(roll)}, displacementY: ?");

        //GlobalRotation = new Vector3(pitch, GlobalRotation.Y, roll);
        //GlobalPosition = new Vector3(GlobalPosition.X, displacementY, GlobalPosition.Z);

        // keep the boat at the surface of the water
        //float boatY = _ocean.GlobalPosition.Y + _size.Y / 2 - DepthInWater;
        //GlobalPosition = new Vector3(GlobalPosition.X, boatY, GlobalPosition.Z);
    }

    private void ApplyForcesFromControls() {
        bool moveForward = Input.IsActionPressed("move_forward");
        bool moveBackward = Input.IsActionPressed("move_backward");
        Vector3 towardsFrontOfBoat = Vector3.Forward.Rotated(Vector3.Up, Rotation.Y);
        if (moveForward && !moveBackward) {
            ApplyCentralForce(towardsFrontOfBoat * EngineForce * Mass);
        }
        if (moveBackward && !moveForward) {
            ApplyCentralForce(towardsFrontOfBoat * -1 * EngineForce * Mass);
        }

        bool turnLeft = Input.IsActionPressed("turn_left");
        bool turnRight = Input.IsActionPressed("turn_right");
        if (turnLeft && !turnRight) {
            ApplyTorque(Vector3.Up * TurnForce * Mass);
        }
        if (turnRight && !turnLeft) {
            ApplyTorque(Vector3.Down * TurnForce * Mass);
        }
    }

    public void ApplyWaterDrag(PhysicsDirectBodyState3D state) {
        float submergedProportion = _depthInWater / _size.Y;
        state.LinearVelocity *= 1 - Mathf.Clamp(WaterDrag * submergedProportion, 0, 1);
        state.AngularVelocity *= 1 - Mathf.Clamp(WaterAngularDrag * submergedProportion, 0, 1);
    }

    // estimates augmentations to the transform, based on the ocean waves
    public (float pitch, float roll, float displacementY) EstimateWaterAugmentations() {
        // get the points around the boat AABB in cross shape (+)
        float halfWidth = _size.X / 2;
        float halfLength = _size.Z / 2;
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
            //GD.Print($"displacement: {displacement}");
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

    public void ResetAboveWater() {
        CallDeferred(nameof(DeferredResetAboveWater));
    }

    public void DeferredResetAboveWater() {
        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;

        Transform3D transform = new Transform3D();
        transform.Basis = Basis.Identity;
        transform.Origin = Vector3.Zero;
        transform = transform.Translated(new Vector3(GlobalPosition.X, _ocean.GlobalPosition.Y + 1f, GlobalPosition.Z));
        transform = transform.Rotated(Vector3.Up, 2);
        Transform = transform;
    }
}

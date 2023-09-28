using Godot;

public partial class Visual : Node3D {
    private Aabb _absBounds;
    private Ocean _ocean;

    public override void _Ready() {
        _absBounds = GetNode<Node3D>("Model").GetNode<MeshInstance3D>("Boat").GetAabb().Abs();
        _ocean = GetTree().Root.GetNode<Ocean>("/root/Main/Ocean");
    }

    public override void _Process(double delta) {
        var aug = EstimateVisualAugmentations();
        GlobalRotation = new Vector3(aug.pitch, GlobalRotation.Y, aug.roll);
        GlobalPosition = new Vector3(GlobalPosition.X, aug.displacementY, GlobalPosition.Z);
    }

    // estimates augmentations to the visual's transforms, based on the ocean waves
    public (float pitch, float roll, float displacementY) EstimateVisualAugmentations() {
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
        GD.Print($"angleFront: {Mathf.RadToDeg(angleFront)}, angleBack: {Mathf.RadToDeg(angleBack)}, angleLeft: {Mathf.RadToDeg(angleLeft)}, angleRight: {Mathf.RadToDeg(angleRight)}");

        float pitch = (angleFront + angleBack) / 2;
        float roll = (angleLeft + angleRight) / 2;


        GD.Print($"pitch: {Mathf.RadToDeg(pitch)}, roll: {Mathf.RadToDeg(roll)}");
        GetNode<MeshInstance3D>("WaterApproximation/FrontSphere").GlobalPosition = front.Rotated(Vector3.Up, GlobalRotation.Y) + GlobalPositionXZ;
        GetNode<MeshInstance3D>("WaterApproximation/BackSphere").GlobalPosition = back.Rotated(Vector3.Up, GlobalRotation.Y) + GlobalPositionXZ;
        GetNode<MeshInstance3D>("WaterApproximation/LeftSphere").GlobalPosition = left.Rotated(Vector3.Up, GlobalRotation.Y) + GlobalPositionXZ;
        GetNode<MeshInstance3D>("WaterApproximation/RightSphere").GlobalPosition = right.Rotated(Vector3.Up, GlobalRotation.Y) + GlobalPositionXZ;

        return (pitch, roll, averageDisplacementY);
    }
}

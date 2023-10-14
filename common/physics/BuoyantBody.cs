using Godot;

public partial class BuoyantBody : RigidBody3D {
    public Vector3 Size = Vector3.One;

    // Buoyancy
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float BuoyancyDamping = 0f;

    public float HorizontalSliceArea { get => Size.X * Size.Z; }
    // drag relies on the depth in water set by buoyancy
    public float DepthInWater { get; private set; } = 0f;

    // Drag
    [Export(PropertyHint.Range, "0,100")]
    public float ConstantLinearDrag = 1.5f;
    [Export(PropertyHint.Range, "0,100")]
    public float ConstantAngularDrag = 1f;
    [Export(PropertyHint.Range, "0,1")]
    public float AirDragCoefficient = 0.5f;
    [Export(PropertyHint.Range, "0,1")]
    public float AirMomentCoefficient = 0.08f;
    [Export(PropertyHint.Range, "0,1")]
    public float WaterDragCoefficient = 0.3f;
    [Export(PropertyHint.Range, "0,1")]
    public float WaterMomentCoefficient = 0.08f;
    [Export(PropertyHint.Range, "0,0.99")]
    public float SubmergedProportionOffset = 0.7f;

    [Export]
    public bool EnableBuoyancy = true;
    [Export]
    public bool EnablePhysicalDrag = true;
    [Export]
    public bool EnableConstantDrag = true;

    public bool DebugLogs = false;

    private float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
    private float _waterDensity = 1000; // kg/m^3
    private float _airDensity = 1.225f; // kg/m^3

    private Ocean _ocean = null!;

    public override void _Ready() {
        _ocean = DependencyInjector.Ref().GetOcean();
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        Transform = Transform.Orthonormalized();

        ApplyBuoyancy();
        ApplyPhysicalDrag(state);
        ApplyConstantDrag(state);
    }

    public void ApplyBuoyancy() {
        if (!EnableBuoyancy) {
            return;
        }

        Node3D waterContactPoints = GetNode<Node3D>("Boat/WaterContactPoints");
        float cumulativeDepth = 0;
        int submergedPoints = 0;
        var children = waterContactPoints.GetChildren();
        float totalBuoyantForce = 0;
        for (int i = 0; i < children.Count; i++) {
            if (!(children[i] is Marker3D)) {
                GD.PrintErr($"Expected Marker3D for water contact point, got {children[i].GetType()}");
                continue;
            }
            Marker3D contactPoint = (Marker3D)children[i];
            // TODO: stop ignoring displacement on XZ plane
            // this should just call a GetHeight(), and the displacement should be internal to the Ocean
            Vector3 waterDisplacement = _ocean.GetDisplacement(new Vector2(contactPoint.GlobalPosition.X, contactPoint.GlobalPosition.Z));
            Vector3 waterContactPoint = new Vector3(contactPoint.GlobalPosition.X, _ocean.GlobalPosition.Y, contactPoint.GlobalPosition.Z) + waterDisplacement;

            // TODO: simplify for now by only considering Y
            if (DebugLogs) {
                GD.Print($"Water height ({waterContactPoint.Y}) = Water {contactPoint.GlobalPosition.Y} + Displacement {waterDisplacement.Y}, boat height = {contactPoint.GlobalPosition.Y}");
            }
            float depth = waterContactPoint.Y - contactPoint.GlobalPosition.Y;

            if (depth > 0) {
                submergedPoints++;
                cumulativeDepth += depth;
                // Archimedes Principle: F = ρgV
                float volumeDisplaced = HorizontalSliceArea * Mathf.Min(depth, Size.Y);
                if (DebugLogs) {
                    GD.Print($"Volume displaced: {volumeDisplaced}, depth: {depth}, horizontal slice area: {HorizontalSliceArea}, boat height: {Size.Y}");
                }
                float buoyancyForce = _waterDensity * _gravity * volumeDisplaced / children.Count;
                totalBuoyantForce += buoyancyForce;
                ApplyForce(Vector3.Up * (1 - BuoyancyDamping) * buoyancyForce, contactPoint.GlobalPosition - GlobalPosition);
            }
        }

        // set depth in water to average of contact points
        if (submergedPoints > 0) {
            DepthInWater = cumulativeDepth / submergedPoints;
        }
        else {
            DepthInWater = 0;
        }

        // verify buoyant force distribution against expected maximum
        float expectedVolumeDisplaced = HorizontalSliceArea * Mathf.Min(DepthInWater, Size.Y);
        float expectedBuoyantForce = _waterDensity * _gravity * expectedVolumeDisplaced;
        if (DebugLogs) {
            GD.Print($"Expected buoyant force: {expectedBuoyantForce}, actual buoyant force: {totalBuoyantForce}, ({totalBuoyantForce / expectedBuoyantForce})");
        }
    }

    public void ApplyPhysicalDrag(PhysicsDirectBodyState3D state) {
        if (!EnableBuoyancy) {
            throw new System.Exception("Buoyancy must be enabled to apply physical drag");
        }
        if (!EnablePhysicalDrag) {
            return;
        }

        float proportionInWater = 0;
        if (DebugLogs) {
            GD.Print($"depth in water: {DepthInWater}");
        }
        if (DepthInWater > 0) {
            proportionInWater = Mathf.Clamp(DepthInWater / Size.Y + SubmergedProportionOffset, 0.001f, 1);
            if (DebugLogs) {
                GD.Print($"submerged proportion: {proportionInWater}");
            }
            DebugTools.Assert(proportionInWater > 0 && proportionInWater <= 1, $"submergedProportion ({proportionInWater}) must be in 0,1");
        }

        float forwardArea = Size.X * Size.Z;
        float linearVelocityLengthSquare = (float)state.LinearVelocity.LengthSquared();
        float AirLinearDrag = 0.5f * _airDensity * forwardArea * AirDragCoefficient * linearVelocityLengthSquare;
        float WaterLinearDrag = 0.5f * _waterDensity * forwardArea * WaterDragCoefficient * linearVelocityLengthSquare;

        float sideArea = Size.Y * Size.Z;
        float angularVelocityLengthSquare = (float)state.AngularVelocity.LengthSquared();
        float AirAngularDrag = 0.5f * _airDensity * sideArea * AirDragCoefficient * angularVelocityLengthSquare;
        float WaterAngularDrag = 0.5f * _waterDensity * sideArea * WaterDragCoefficient * angularVelocityLengthSquare;

        float linearDrag = proportionInWater * WaterLinearDrag + (1 - proportionInWater) * AirLinearDrag;
        float angularDrag = proportionInWater * WaterAngularDrag + (1 - proportionInWater) * AirAngularDrag;

        if (DebugLogs) {
            GD.Print($"linear drag: {linearDrag}, angular drag: {angularDrag}");
        }
        ApplyCentralForce(-state.LinearVelocity * linearDrag * state.Step);
        ApplyTorque(-state.AngularVelocity * angularDrag * state.Step);
    }

    public void ApplyConstantDrag(PhysicsDirectBodyState3D state) {
        if (!EnableConstantDrag) {
            return;
        }
        state.LinearVelocity *= 1 - Mathf.Clamp(ConstantLinearDrag * state.Step, 0, 1);
        state.AngularVelocity *= 1 - Mathf.Clamp(ConstantAngularDrag * state.Step, 0, 1);
    }
}

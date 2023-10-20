using Godot;
using System.Collections.Generic;

public partial class BuoyantBody : RigidBody3D {
    [Export]
    public Vector3 Size {
        get => _size;
        set {
            _size = value;
            if (_ready && string.IsNullOrEmpty(WaterContactPointsNodePath)) {
                DefaultWaterContactPoints();
            }
        }
    }

    private Vector3 _size = Vector3.One;

    // Buoyancy
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float BuoyancyDamping = 0f;

    public float HorizontalSliceArea { get => Size.X * Size.Z; }
    // drag relies on the depth in water set by buoyancy
    public float DepthInWater { get; private set; } = 0f;

    [Export]
    public string? WaterContactPointsNodePath {
        get => _waterContactPointsNodePath;
        set {
            GD.Print($"Setting water contact points node path: {value}");
            _waterContactPointsNodePath = value;
            if (string.IsNullOrEmpty(value)) {
                DefaultWaterContactPoints();
            }
            else if (_ready) {
                ValidateWaterContactPoints();
            }
        }
    }
    private string? _waterContactPointsNodePath = null;
    private string _defaultWaterContactPointsNodePath = "DefaultWaterContactPoints";
    private bool _waterContactPointsReady = false;

    private bool _ready = false;

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
    public float SubmergedProportionOffset = 0.5f;

    [Export]
    public bool EnableBuoyancy = true;
    [Export]
    public bool EnablePhysicalDrag = true;
    [Export]
    public bool EnableConstantDrag = true;

    [Export]
    public bool DebugLogs = false;

    private float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
    private float _waterDensity = 1000; // kg/m^3
    private float _airDensity = 1.225f; // kg/m^3

    private Ocean _ocean = null!;

    public override void _Ready() {
        _ocean = DependencyInjector.Ref().GetOcean();

        if (WaterContactPointsNodePath == null) {
            DefaultWaterContactPoints();
        }
        if (!string.IsNullOrEmpty(WaterContactPointsNodePath)) {
            ValidateWaterContactPoints();
        }

        if (CenterOfMassMode != CenterOfMassModeEnum.Custom) {
            CenterOfMassMode = CenterOfMassModeEnum.Custom;
            CenterOfMass = new Vector3(0, -Size.Y / 2, 0);
            GD.Print($"Setting default center of mass: {CenterOfMass} for {Name}");
        }
        if (Mass == 1f) {
            GD.PrintErr("Looks like you forgot to set the mass on your BuoyantBody");
        }

        _ready = true;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        Transform = Transform.Orthonormalized();

        ApplyBuoyancy();
        ApplyPhysicalDrag(state);
        ApplyConstantDrag(state);
    }

    private void ApplyBuoyancy() {
        if (!EnableBuoyancy) {
            return;
        }
        if (!_waterContactPointsReady) {
            return;
        }
        if (string.IsNullOrEmpty(WaterContactPointsNodePath)) {
            throw new System.Exception("WaterContactPointsNodePath must be set");
        }

        Node3D waterContactPointsNode = GetNode<Node3D>(WaterContactPointsNodePath);
        if (waterContactPointsNode == null) {
            throw new System.Exception($"WaterContactPointsNodePath ({WaterContactPointsNodePath}) is null");
        }
        var waterContactPoints = waterContactPointsNode.GetChildren();
        List<float> waterContactPointDepths = new List<float>();
        // compute depths first
        for (int i = 0; i < waterContactPoints.Count; i++) {
            Marker3D? contactPoint = waterContactPoints[i] as Marker3D;
            if (contactPoint == null) {
                throw new System.Exception($"WaterContactPointsNodePath ({WaterContactPointsNodePath}) child {i} is not a Marker3D");
            }

            // TODO: stop ignoring displacement on XZ plane
            // this should just call a GetHeight(), and the displacement should be internal to the Ocean
            Vector3 waterDisplacement = _ocean.GetDisplacement(new Vector2(contactPoint.GlobalPosition.X, contactPoint.GlobalPosition.Z));
            Vector3 waterContactPoint = new Vector3(contactPoint.GlobalPosition.X, _ocean.GlobalPosition.Y, contactPoint.GlobalPosition.Z) + waterDisplacement;

            // TODO: simplify for now by only considering Y
            if (DebugLogs) {
                GD.Print($"Water height ({waterContactPoint.Y}) = Water {contactPoint.GlobalPosition.Y} + Displacement {waterDisplacement.Y}, boat height = {contactPoint.GlobalPosition.Y}");
            }
            float depth = waterContactPoint.Y - contactPoint.GlobalPosition.Y;
            waterContactPointDepths.Add(depth);
        }

        // adjust forces based on number of submerged points
        float cumulativeDepth = 0;
        int noSubmergedPoints = 0;
        for (int i = 0; i < waterContactPoints.Count; i++) {
            float depth = waterContactPointDepths[i];
            if (depth > 0) {
                noSubmergedPoints++;
                cumulativeDepth += depth;
            }
        }

        // apply buoyancy forces
        float totalBuoyantForce = 0;
        for (int i = 0; i < waterContactPoints.Count; i++) {
            Marker3D contactPoint = (Marker3D)waterContactPoints[i];
            float depth = waterContactPointDepths[i];
            if (depth > 0) {

                // Archimedes Principle: F = ρgV
                float volumeDisplaced = HorizontalSliceArea * Mathf.Min(depth, Size.Y);
                if (DebugLogs) {
                    GD.Print($"Volume displaced: {volumeDisplaced}, depth: {depth}, horizontal slice area: {HorizontalSliceArea}, boat height: {Size.Y}");
                }
                float buoyancyForce = _waterDensity * _gravity * volumeDisplaced / noSubmergedPoints;
                totalBuoyantForce += buoyancyForce;
                ApplyForce(Vector3.Up * (1 - BuoyancyDamping) * buoyancyForce, contactPoint.GlobalPosition - GlobalPosition);
            }
        }

        // set depth in water to average of contact points
        if (noSubmergedPoints > 0) {
            DepthInWater = cumulativeDepth / noSubmergedPoints;
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

    private void ApplyPhysicalDrag(PhysicsDirectBodyState3D state) {
        if (!EnablePhysicalDrag) {
            return;
        }
        if (!EnableBuoyancy) {
            GD.PrintErr("Buoyancy must be enabled to apply physical drag");
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

    private void ApplyConstantDrag(PhysicsDirectBodyState3D state) {
        if (!EnableConstantDrag) {
            return;
        }
        state.LinearVelocity *= 1 - Mathf.Clamp(ConstantLinearDrag * state.Step, 0, 1);
        state.AngularVelocity *= 1 - Mathf.Clamp(ConstantAngularDrag * state.Step, 0, 1);
    }

    private void DefaultWaterContactPoints() {
        if (!string.IsNullOrEmpty(_waterContactPointsNodePath)) {
            return;
        }

        Node3D? waterContactPoints = GetNodeOrNull<Node3D>(_defaultWaterContactPointsNodePath);
        if (waterContactPoints != null) {
            foreach (Node child in GetNode<Node3D>(_defaultWaterContactPointsNodePath).GetChildren()) {
                child.QueueFree();
            }
            waterContactPoints.Ready -= ValidateWaterContactPoints;
            waterContactPoints.Name = _defaultWaterContactPointsNodePath + "_old";
            waterContactPoints.QueueFree();
        }
        waterContactPoints = new Node3D() {
            Name = _defaultWaterContactPointsNodePath,
            Position = new Vector3(0, -Size.Y / 2, 0),
        };
        // set private field directly since we have alternative validation for generated water contact points
        _waterContactPointsNodePath = _defaultWaterContactPointsNodePath;

        Vector3[] offsets = new Vector3[] {
            Vector3.Zero,
            Vector3.Right * Size.X,
            Vector3.Left * Size.X,
            Vector3.Forward * Size.Z,
            Vector3.Back * Size.Z,
            Vector3.Zero + Vector3.Up * Size.Y,
            Vector3.Right * Size.X + Vector3.Up * Size.Y,
            Vector3.Left * Size.X + Vector3.Up * Size.Y,
            Vector3.Forward * Size.Z + Vector3.Up * Size.Y,
            Vector3.Back * Size.Z + Vector3.Up * Size.Y,
        };
        foreach (Vector3 offset in offsets) {
            Marker3D m = new Marker3D() {
                Position = offset,
            };
            GD.Print($"Adding water contact point at {m.Position}");
            waterContactPoints.AddChild(m);
        }

        waterContactPoints.Ready += ValidateWaterContactPoints;
        AddChild(waterContactPoints);
    }

    private void ValidateWaterContactPoints() {
        if (string.IsNullOrEmpty(WaterContactPointsNodePath)) {
            return;
        }
        GD.Print($"Validating water contact points: {WaterContactPointsNodePath}");
        var node = GetNode(WaterContactPointsNodePath);
        if (!(node is Node3D)) {
            throw new System.Exception($"WaterContactPointsFromChildren node3DPath ({WaterContactPointsNodePath}) is not a Node3D");
        }
        var children = node.GetChildren();
        foreach (Node child in children) {
            if (!(child is Marker3D)) {
                GD.PrintErr("WaterContactPointsFromNode3D child is not a Marker3D");
            }
        }
        _waterContactPointsReady = true;
    }
}

using Godot;
using System.Collections.Generic;

public partial class Player : RigidBody3D {
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float BuoyancyDamping = 0f;

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

    // F = ma, for a in m/s^2
    [Export]
    public float EngineForce = 9f;
    // rad/s^2
    [Export]
    public float TurnForce = 2.5f;

    [Export]
    public bool DisableControls = false;
    [Export]
    public float PositionChangedSignificanceEpsilon = Mathf.Pow(2f, 2);
    private Vector3 _lastSignificantPosition = Vector3.Zero;
    [Export]
    public bool DebugLogs = false;

    private Ocean _ocean = null!;

    private float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
    private float _waterDensity = 1000; // kg/m^3
    private float _airDensity = 1.225f; // kg/m^3
    private float _horizontalSliceArea;
    private float _depthInWater = 1f;
    private Vector3 _size;

    private static readonly List<string> _moveActions = new List<string>() {
            "move_forward",
            "move_backward",
            "turn_left",
            "turn_right",
        };

    [Signal]
    public delegate void PositionChangedSignificantlyEventHandler(Vector3 position);

    public override void _Ready() {
        _ocean = DependencyInjector.Ref().GetOcean();

        // row boat
        //_size = new Vector3(1f, 0.5f, 2.5f);
        //Mass = 150f;

        // simple boat
        _size = new Vector3(2f, 3f, 2f);

        _horizontalSliceArea = _size.X * _size.Y;
        if (DebugLogs) {
            GD.Print($"Boat Size: {_size}. Horizontal slice area: {_horizontalSliceArea}");
        }

        MeshInstance3D boundingBox = GetNode<MeshInstance3D>("BoundingBox");
        if (boundingBox != null && boundingBox.Mesh is BoxMesh) {
            BoxMesh boxMesh = (BoxMesh)boundingBox.Mesh;
            boxMesh.Size = _size;
        }
    }

    public override void _Process(double delta) {
        // update lagging Player position for the Ocean
        if (GlobalPosition.DistanceSquaredTo(_lastSignificantPosition) > PositionChangedSignificanceEpsilon) {
            EmitSignal(SignalName.PositionChangedSignificantly, GlobalPosition);
            _lastSignificantPosition = GlobalPosition;
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        Transform = Transform.Orthonormalized();

        ApplyForcesFromControls();
        ApplyBuoyancy();
        ApplyPhysicalDrag(state);
        ApplyConstantDrag(state);
    }

    private void ApplyForcesFromControls() {
        if (DisableControls) {
            return;
        }
        if (_depthInWater <= 0) {
            return;
        }

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

    private void ApplyBuoyancy() {
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
                float volumeDisplaced = _horizontalSliceArea * Mathf.Min(depth, _size.Y);
                if (DebugLogs) {
                    GD.Print($"Volume displaced: {volumeDisplaced}, depth: {depth}, horizontal slice area: {_horizontalSliceArea}, boat height: {_size.Y}");
                }
                float buoyancyForce = _waterDensity * _gravity * volumeDisplaced / children.Count;
                totalBuoyantForce += buoyancyForce;
                ApplyForce(Vector3.Up * (1 - BuoyancyDamping) * buoyancyForce, contactPoint.GlobalPosition - GlobalPosition);
            }
        }

        // set depth in water to average of contact points
        if (submergedPoints > 0) {
            _depthInWater = cumulativeDepth / submergedPoints;
        }
        else {
            _depthInWater = 0;
        }

        // verify buoyant force distribution against expected maximum
        float expectedVolumeDisplaced = _horizontalSliceArea * Mathf.Min(_depthInWater, _size.Y);
        float expectedBuoyantForce = _waterDensity * _gravity * expectedVolumeDisplaced;
        if (DebugLogs) {
            GD.Print($"Expected buoyant force: {expectedBuoyantForce}, actual buoyant force: {totalBuoyantForce}, ({totalBuoyantForce / expectedBuoyantForce})");
        }
    }

    public void ApplyPhysicalDrag(PhysicsDirectBodyState3D state) {
        float proportionInWater = 0;
        if (DebugLogs) {
            GD.Print($"depth in water: {_depthInWater}");
        }
        if (_depthInWater > 0) {
            proportionInWater = Mathf.Clamp(_depthInWater / _size.Y + SubmergedProportionOffset, 0.001f, 1);
            if (DebugLogs) {
                GD.Print($"submerged proportion: {proportionInWater}");
            }
            DebugTools.Assert(proportionInWater > 0 && proportionInWater <= 1, $"submergedProportion ({proportionInWater}) must be in (0, 1]");
        }

        float forwardArea = _size.X * _size.Z;
        float linearVelocityLengthSquare = (float)state.LinearVelocity.LengthSquared();
        float AirLinearDrag = 0.5f * _airDensity * forwardArea * AirDragCoefficient * linearVelocityLengthSquare;
        float WaterLinearDrag = 0.5f * _waterDensity * forwardArea * WaterDragCoefficient * linearVelocityLengthSquare;

        float sideArea = _size.Y * _size.Z;
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
        state.LinearVelocity *= 1 - Mathf.Clamp(ConstantLinearDrag * state.Step, 0, 1);
        state.AngularVelocity *= 1 - Mathf.Clamp(ConstantAngularDrag * state.Step, 0, 1);
    }

    public void ResetAboveWater(bool relocate = false, Vector2 globalXZ = default, float globalRotationY = 0f) {
        if (relocate) {
            CallDeferred(nameof(DeferredResetAboveWater), true, globalXZ, globalRotationY);
        }
        else {
            CallDeferred(nameof(DeferredResetAboveWater));
        }
    }

    // CallDeferred doesn't seem to understand default arguments, so we need to overload
    private void DeferredResetAboveWater() {
        DeferredResetAboveWater(false);
    }

    private void DeferredResetAboveWater(bool relocate = false, Vector2 globalXZ = default, float globalRotationY = 0f) {
        float yaw = GlobalRotation.Y;
        if (relocate) {
            yaw = globalRotationY;
        }

        Vector3 translation = new Vector3(GlobalPosition.X, _ocean.GlobalPosition.Y + 1f, GlobalPosition.Y);
        if (relocate) {
            translation.X = globalXZ.X;
            translation.Z = globalXZ.Y;
        }

        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;

        Transform3D transform = new Transform3D();
        // ignore the linter. you must set the basis and origin
        transform.Basis = Basis.Identity;
        transform.Origin = Vector3.Zero;
        transform = transform.Rotated(Vector3.Up, yaw);
        transform = transform.Translated(translation);
        Transform = transform;
    }

    public void SetControlsDisabled(bool controlsDisabled) {
        DisableControls = controlsDisabled;
    }

    public bool IsMoving() {
        bool isMoving = false;
        foreach (string action in _moveActions) {
            if (Input.IsActionPressed(action)) {
                isMoving = true;
                break;
            }
        }
        return isMoving;
    }
}

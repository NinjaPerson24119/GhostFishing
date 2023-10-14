using Godot;

public partial class PublicBuoyantBody : RigidBody3D {
    [Export]
    public Vector3 Size = Vector3.One;

    // Buoyancy
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float BuoyancyDamping = 0f;

    [Export]
    public virtual float DefaultWaterContactPointsRadius { get; set; }
    [Export]
    public virtual string? WaterContactPointsNodePath { get; set; }

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

    [Export]
    public bool DebugLogs = false;
}

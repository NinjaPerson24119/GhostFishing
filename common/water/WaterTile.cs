using Godot;

internal partial class WaterTile : MeshInstance3D {
    [Export]
    public float WaterDepth {
        get {
            return _waterDepth;
        }
        set {
            _waterDepth = value;
            QueueReconfigureShaders();
        }
    }
    private float _waterDepth = 1000f;

    [Export]
    public bool WaterTileDebugLogs = false;

    [Export]
    public int Subdivisions {
        get {
            return _subdivisions;
        }
        set {
            _subdivisions = value;
            QueueReconfigureMesh();
        }
    }
    private int _subdivisions = 200;

    [Export]
    public bool NoDisplacement {
        get {
            return _noDisplacement;
        }
        set {
            _noDisplacement = value;
            QueueReconfigureShaders();
        }
    }
    private bool _noDisplacement = false;

    [ExportGroup("Surface Perturbation")]
    [Export]
    public float SurfaceNoiseScale {
        get {
            return _surfaceNoiseScale;
        }
        set {
            _surfaceNoiseScale = value;
            QueueReconfigureShaders();
        }
    }
    private float _surfaceNoiseScale = 10.0f;
    [Export]
    public float SurfaceHeightScale {
        get {
            return _surfaceHeightScale;
        }
        set {
            _surfaceHeightScale = value;
            QueueReconfigureShaders();
        }
    }
    private float _surfaceHeightScale = 0.2f;
    [Export]
    public float SurfaceTimeScale {
        get {
            return _surfaceTimeScale;
        }
        set {
            _surfaceTimeScale = value;
            QueueReconfigureShaders();
        }
    }
    private float _surfaceTimeScale = 0.025f;
    private static Image? _surfaceNoise;

    public WaveSet? WavesConfig;

    private bool _queueReconfigureShaders = false;
    private bool _queueReconfigureMesh = false;

    // load once and duplicate to avoid reloading the shader every time a new water tile is created
    private static ShaderMaterial _loadedMaterial = GD.Load<ShaderMaterial>("res://common/water/Water.material");
    private ShaderMaterial _material = (ShaderMaterial)_loadedMaterial.Duplicate();

    // this must match the shader, do not adjust it to change the number of active waves
    // this represents the maximum supported number of waves in the shader
    private const int maxWaves = 12;
    private static RefCountedAssetSpectrum<int, PlaneMesh> refCountedMeshes = new RefCountedAssetSpectrum<int, PlaneMesh>(BuildMesh);

    private RealClock _realClock = null!;

    public override void _Ready() {
        _realClock = RealClock.Ref();

        if (WavesConfig == null) {
            GD.Print("Generating default wave set because none was provided.");
            WaveSetConfig config = WaveSet.BuildConfig(10, Mathf.Pi, WaterDepth);
            WavesConfig = new WaveSet(config);
        }
        ConfigureMesh();
        ConfigureShader();
    }

    public override void _Process(double delta) {
        _material.SetShaderParameter("wave_time", _realClock.RealTime);

        if (_queueReconfigureShaders) {
            ConfigureShader();
        }
        if (_queueReconfigureMesh) {
            ConfigureMesh();
        }
    }

    private void ConfigureMesh() {
        if (Mesh == null || _queueReconfigureMesh) {
            // verify that we have a plane
            if (Mesh != null && Mesh is PlaneMesh) {
                var planeMesh = (PlaneMesh)Mesh;
                // if the subdivisions have changed, unref the old mesh
                if (planeMesh.SubdivideWidth != Subdivisions) {
                    refCountedMeshes.Unref(planeMesh.SubdivideWidth);
                    Mesh = null;
                }
            }
            Mesh = refCountedMeshes.GetAndRef(Subdivisions);
            Mesh.SurfaceSetMaterial(0, _material);
        }
        _queueReconfigureMesh = false;
    }

    private static PlaneMesh BuildMesh(int resolution) {
        PlaneMesh mesh = new PlaneMesh() {
            Size = new Vector2(1, 1),
            SubdivideDepth = resolution,
            SubdivideWidth = resolution,
        };
        return mesh;
    }

    private void ConfigureShader() {
        _queueReconfigureShaders = false;

        _material.SetShaderParameter("no_displacement", NoDisplacement);
        ConfigureShaderSurfacePerturbation();
        ConfigureShaderGerstnerWaves();
    }

    private void ConfigureShaderSurfacePerturbation() {
        if (_surfaceNoise == null) {
            var noiseTexture = (NoiseTexture2D)_material.GetShaderParameter("wave");
            _surfaceNoise = noiseTexture.Noise.GetSeamlessImage(512, 512);
        }

        _material.SetShaderParameter("noise_scale", _surfaceNoiseScale);
        _material.SetShaderParameter("height_scale", _surfaceHeightScale);
        _material.SetShaderParameter("time_scale", _surfaceTimeScale);
    }

    private void ConfigureShaderGerstnerWaves() {
        float[] amplitude = new float[maxWaves];
        float[] k = new float[maxWaves];
        float[] kX = new float[maxWaves];
        float[] kZ = new float[maxWaves];
        float[] omega = new float[maxWaves];
        float[] phi = new float[maxWaves];
        float[] productOperandX = new float[maxWaves];
        float[] productOperandZ = new float[maxWaves];

        // h == 0 is a singularity in the Gerstner wave function, and negative values make no sense
        DebugTools.Assert(WavesConfig != null, "WavesConfig must be set before configuring the shader");
        if (WaterDepth > 0 && WavesConfig != null) {
            // only fill the first n waves
            // default k == 0 tells the shader to ignore the wave
            DebugTools.Assert(WavesConfig.waves.Count <= maxWaves, $"Too many waves configured. Max supported is {maxWaves}");
            int wavesToRender = Mathf.Min(WavesConfig.waves.Count, maxWaves);
            for (int i = 0; i < wavesToRender; i++) {
                Wave w = WavesConfig.waves[i];
                amplitude[i] = w.amplitude;
                k[i] = w.k;
                kX[i] = w.kX;
                kZ[i] = w.kZ;
                omega[i] = w.angularFrequency;
                phi[i] = w.phaseShift;
                productOperandX[i] = w.productOperandX;
                productOperandZ[i] = w.productOperandZ;

                if (WaterTileDebugLogs) {
                    GD.Print($"Generated wave {i}:");
                    GD.Print($"\tamplitude: {amplitude[i]}");
                    GD.Print($"\tk: {k[i]}");
                    GD.Print($"\tkX: {kX[i]}");
                    GD.Print($"\tkZ: {kZ[i]}");
                    GD.Print($"\tomega: {omega[i]}");
                    GD.Print($"\tphi: {phi[i]}");
                    GD.Print($"\tkA: {k[i] * amplitude[i]}");
                    GD.Print($"\twindAngle: {w.windAngle}");
                }
            }
        }

        _material.SetShaderParameter("gerstner_k", k);
        _material.SetShaderParameter("gerstner_k_x", kX);
        _material.SetShaderParameter("gerstner_k_z", kZ);
        _material.SetShaderParameter("gerstner_a", amplitude);
        _material.SetShaderParameter("gerstner_omega", omega);
        _material.SetShaderParameter("gerstner_phi", phi);
        _material.SetShaderParameter("gerstner_product_operand_x", productOperandX);
        _material.SetShaderParameter("gerstner_product_operand_z", productOperandZ);
    }

    // returns the displacement of the water at the given world position
    // this always should match the vertex shader algorithm for physics to be visually consistent
    public Vector3 GetDisplacement(Vector2 globalXZ) {
        if (NoDisplacement) {
            return Vector3.Zero;
        }
        DebugTools.Assert(WavesConfig != null, "WavesConfig must be set before getting displacement");
        DebugTools.Assert(_surfaceNoise != null, "Surface noise must be set before getting displacement");
        if (WavesConfig == null || _surfaceNoise == null) {
            return Vector3.Zero;
        }

        // calculate Gerstner portion of displacement
        Gerstner gerstner = new Gerstner(WavesConfig);
        Vector3 gerstnerDisplacement = gerstner.Displacement(globalXZ.X, globalXZ.Y, (float)_realClock.RealTime);

        // calculate noise portion of displacement
        Vector3 normal = gerstner.Normal(globalXZ.X, globalXZ.Y, (float)_realClock.RealTime);

        // ignore how shader smooths noise at tile edges. always compute full noise
        int uvX = (int)Mathf.Wrap(globalXZ.X / _surfaceNoiseScale + _realClock.RealTime * _surfaceTimeScale, 0.0, 1.0);
        int uvY = (int)Mathf.Wrap(globalXZ.Y / _surfaceNoiseScale + _realClock.RealTime * _surfaceTimeScale, 0.0, 1.0);
        float noiseHeight = _surfaceNoise.GetPixel(uvX * _surfaceNoise.GetWidth(), uvY * _surfaceNoise.GetHeight()).R;

        return gerstnerDisplacement + normal * noiseHeight * _surfaceHeightScale;
    }

    public void SetDebugVisuals(bool enabled) {
        _material.SetShaderParameter("debug_visuals", enabled);
    }

    public void QueueReconfigureShaders() {
        if (WaterTileDebugLogs) {
            GD.Print("Queueing reconfigure water tile shaders");
        }
        _queueReconfigureShaders = true;
    }

    private void QueueReconfigureMesh() {
        if (WaterTileDebugLogs) {
            GD.Print("Queueing reconfigure water tile mesh");
        }
        _queueReconfigureMesh = true;
    }
}

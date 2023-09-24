using Godot;

public struct ShaderSurfacePerturbationConfig {
    public Image noise;
    public float noiseScale;
    public float heightScale;
    public float timeScale;
}

public partial class WaterTile : MeshInstance3D {
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

    public WaveSet WavesConfig;

    private bool _queueReconfigureShaders = false;
    private bool _queueReconfigureMesh = false;

    // load once and duplicate to avoid reloading the shader every time a new water tile is created
    private static ShaderMaterial _loadedMaterial = GD.Load<ShaderMaterial>("res://common/water/Water.material");
    private ShaderMaterial _material = _loadedMaterial.Duplicate() as ShaderMaterial;

    private static ShaderSurfacePerturbationConfig surfaceConfig = new ShaderSurfacePerturbationConfig() {
        noiseScale = 10.0f,
        heightScale = 0.15f,
        timeScale = 0.025f,
    };
    // this must match the shader, do not adjust it to change the number of active waves
    // this represents the maximum supported number of waves in the shader
    private const int maxWaves = 30;
    private static RefCountedAssetSpectrum<int, PlaneMesh> refCountedMeshes = new RefCountedAssetSpectrum<int, PlaneMesh>(BuildMesh);

    public override void _Ready() {
        if (WavesConfig == null) {
            WaveSetConfig config = WaveSet.BuildConfig(maxWaves, Mathf.Pi, WaterDepth);
            WavesConfig = new WaveSet(config);
        }
        ConfigureMesh();
        ConfigureShader();
    }

    public override void _Process(double delta) {
        _material.SetShaderParameter("wave_time", GameClock.Time);

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

        ConfigureShaderSurfacePerturbation();
        ConfigureShaderGerstnerWaves();
    }

    private void ConfigureShaderSurfacePerturbation() {
        if (surfaceConfig.noise == null) {
            var noiseTexture = (NoiseTexture2D)_material.GetShaderParameter("wave");
            surfaceConfig.noise = noiseTexture.Noise.GetSeamlessImage(512, 512);
        }

        _material.SetShaderParameter("noise_scale", surfaceConfig.noiseScale);
        _material.SetShaderParameter("height_scale", surfaceConfig.heightScale);
        _material.SetShaderParameter("time_scale", surfaceConfig.timeScale);
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
        if (WaterDepth > 0) {
            // only fill the first n waves
            // default k == 0 tells the shader to ignore the wave
            DebugTools.Assert(WavesConfig.waves.Count <= maxWaves, $"Too many waves configured. Max supported is {maxWaves}");
            for (int i = 0; i < Mathf.Min(WavesConfig.waves.Count, maxWaves); i++) {
                amplitude[i] = WavesConfig.waves[i].amplitude;
                k[i] = WavesConfig.waves[i].k;
                kX[i] = WavesConfig.waves[i].kX;
                kZ[i] = WavesConfig.waves[i].kZ;
                omega[i] = WavesConfig.waves[i].angularFrequency;
                phi[i] = WavesConfig.waves[i].phaseShift;

                if (WaterTileDebugLogs) {
                    GD.Print($"Generated wave {i}:");
                    GD.Print($"\tamplitude: {amplitude[i]}");
                    GD.Print($"\tk: {k[i]}");
                    GD.Print($"\tkX: {kX[i]}");
                    GD.Print($"\tkZ: {kZ[i]}");
                    GD.Print($"\tomega: {omega[i]}");
                    GD.Print($"\tphi: {phi[i]}");
                    GD.Print($"\tkA: {k[i] * amplitude[i]}");
                    GD.Print($"\twindAngle: {WavesConfig.waves[i].windAngle}");
                }

                // precompute these for performance
                // these are product operands in the x and z displacement sums
                productOperandX[i] = (kX[i] / k[i]) * (amplitude[i] / Mathf.Tanh(k[i] * WaterDepth));
                productOperandZ[i] = (kZ[i] / k[i]) * (amplitude[i] / Mathf.Tanh(k[i] * WaterDepth));
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

    // returns the height of the water at the given world position
    // this always should match the vertex shader algorithm for physics to be visually consistent
    public float GetHeight(Vector3 worldPosition) {
        // TODO: update to Gerstner waves
        int uvX = (int)Mathf.Wrap(worldPosition.X / surfaceConfig.noiseScale + GameClock.Time * surfaceConfig.timeScale, 0.0, 1.0);
        int uvY = (int)Mathf.Wrap(worldPosition.Z / surfaceConfig.noiseScale + GameClock.Time * surfaceConfig.timeScale, 0.0, 1.0);
        return GlobalPosition.Y + surfaceConfig.noise.GetPixel(uvX * surfaceConfig.noise.GetWidth(), uvY * surfaceConfig.noise.GetHeight()).R * surfaceConfig.heightScale;
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

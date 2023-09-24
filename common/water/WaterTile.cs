using System.Collections;
using Godot;

public struct ShaderSurfacePerturbationConfig {
    public Image noise;
    public float noiseScale;
    public float heightScale;
    public float timeScale;
}

// Owns the shading and dynamics of a single water tile
public partial class WaterTile : MeshInstance3D {
    // this must match the shader, do not adjust it to change the number of active waves
    // this represents the maximum supported number of waves in the shader
    private const int maxWaves = 10;

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
    private float _waterDepth;

    [Export]
    public bool WaterTileDebugLogs = false;

    public ShaderMaterial Material;
    public WaveSet WavesConfig;
    private bool _queueReconfigureShaders = false;

    private static ShaderSurfacePerturbationConfig surfaceConfig = new ShaderSurfacePerturbationConfig() {
        noiseScale = 10.0f,
        heightScale = 0.15f,
        timeScale = 0.025f,
    };

    public override void _Ready() {
        ConfigureShader();
    }

    public override void _Process(double delta) {
        double waveTime = GetParent<Ocean>().WaveTime;
        Material.SetShaderParameter("wave_time", waveTime);
    }

    private void ConfigureShader() {
        _queueReconfigureShaders = false;

        ConfigureShaderSurfacePerturbation();
        ConfigureShaderGerstnerWaves();
    }

    private void ConfigureShaderSurfacePerturbation() {
        var noiseTexture = (NoiseTexture2D)Material.GetShaderParameter("wave");
        surfaceConfig.noise = noiseTexture.Noise.GetSeamlessImage(512, 512);

        Material.SetShaderParameter("noise_scale", surfaceConfig.noiseScale);
        Material.SetShaderParameter("height_scale", surfaceConfig.heightScale);
        Material.SetShaderParameter("time_scale", surfaceConfig.timeScale);
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

        Material.SetShaderParameter("gerstner_k", k);
        Material.SetShaderParameter("gerstner_k_x", kX);
        Material.SetShaderParameter("gerstner_k_z", kZ);
        Material.SetShaderParameter("gerstner_a", amplitude);
        Material.SetShaderParameter("gerstner_omega", omega);
        Material.SetShaderParameter("gerstner_phi", phi);
        Material.SetShaderParameter("gerstner_product_operand_x", productOperandX);
        Material.SetShaderParameter("gerstner_product_operand_z", productOperandZ);
    }

    // returns the height of the water at the given world position
    // this always should match the vertex shader algorithm for physics to be visually consistent
    public float GetHeight(Vector3 worldPosition, double waveTime) {
        // TODO: update to Gerstner waves
        int uvX = (int)Mathf.Wrap(worldPosition.X / surfaceConfig.noiseScale + waveTime * surfaceConfig.timeScale, 0.0, 1.0);
        int uvY = (int)Mathf.Wrap(worldPosition.Z / surfaceConfig.noiseScale + waveTime * surfaceConfig.timeScale, 0.0, 1.0);
        return GlobalPosition.Y + surfaceConfig.noise.GetPixel(uvX * surfaceConfig.noise.GetWidth(), uvY * surfaceConfig.noise.GetHeight()).R * surfaceConfig.heightScale;
    }

    public void SetDebugVisuals(bool enabled) {
        Material.SetShaderParameter("debug_visuals", enabled);
    }

    private void QueueReconfigureShaders() {
        if (WaterTileDebugLogs) {
            GD.Print("Queueing reconfigure water tile shaders");
        }
        _queueReconfigureShaders = true;
    }
}

using Godot;
using System.Collections.Generic;

// Gerstner Wave
public class Wave {
    static float gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
    public float amplitude { get; private set; }
    public float k { get; private set; }
    public float kX { get; private set; }
    public float kZ { get; private set; }
    public float angularFrequency { get; private set; }
    public float phaseShift { get; private set; }
    public float windAngle { get; private set; }

    public Wave(float wavelength, float amplitude, float windAngle, float phaseShift, float waterDepth) {
        // forward prop for debugging
        this.windAngle = windAngle;

        // k is the wave number (magnitude of the wave vector)
        // (kX, kZ) is the wave vector, which is the direction of the wave
        GD.Print($"Generating wave with wavelength {wavelength}, amplitude {amplitude}, wind angle {windAngle}, phase shift {phaseShift}");
        kX = 2 * Mathf.Pi / wavelength * Mathf.Cos(windAngle);
        kZ = 2 * Mathf.Pi / wavelength * Mathf.Sin(windAngle);
        k = Mathf.Sqrt(kX * kX + kZ * kZ);
        DebugTools.Assert(k * amplitude < 1, $"Wave function does not satisfy kA < 1. k: {k}, a: {amplitude}. Max amplitude for this wave is {1 / k}");

        this.amplitude = amplitude;
        angularFrequency = Mathf.Sqrt(gravity * k * Mathf.Tanh(k * waterDepth));
        this.phaseShift = phaseShift;
    }
}

public struct WaveSetConfig {
    public int noWaves;

    public float wavelengthAverage;
    public float wavelengthStdDev;

    // amplitude is proportional to wavelength so does not get an independent variance
    public float amplitudeAverage;

    public float windAngleAverage;
    public float windAngleStdDev;

    public float waterDepth;
}

public class WaveSet {
    public WaveSetConfig config { get; private set; }

    private float _amplitudeWavelengthRatio;
    private RandomNumberGenerator _random;
    public List<Wave> waves { get; private set; } = new List<Wave>();

    public WaveSet(WaveSetConfig config) {
        _random = new RandomNumberGenerator();
        _random.Randomize();
        Configure(config);
    }

    // configures the wave set for sampling, and produces and initial set of waves
    public void Configure(WaveSetConfig config) {
        this.config = config;
        _amplitudeWavelengthRatio = config.amplitudeAverage / config.wavelengthAverage;

        if (waves.Count != config.noWaves) {
            SampleAll();
        }
    }

    // fills the wave set with new samples
    public void SampleAll() {
        waves.Clear();
        for (int i = 0; i < config.noWaves; i++) {
            waves.Add(SampleWave(i));
        }
    }

    // replaces a single wave with a new sample
    // this can be used to gradually update the wave set over time after changing the configuration
    public void ResampleOnce() {
        int index = _random.RandiRange(0, config.noWaves - 1);
        waves[index] = SampleWave(index);
    }

    private Wave SampleWave(int waveIndex) {
        float wavelength = _random.Randfn(config.wavelengthAverage, config.wavelengthStdDev);
        wavelength = Mathf.Clamp(wavelength, config.wavelengthAverage / 2, config.wavelengthAverage * 2);

        // sample proportionally with a / l ~ aAverage / lAverage
        float amplitude = _amplitudeWavelengthRatio * wavelength;
        DebugTools.Assert(Mathf.Abs(amplitude / wavelength - _amplitudeWavelengthRatio) < 0.01f, $"Amplitude-wavelength ratio is not being sampled correctly. a: {amplitude}, l: {wavelength}");

        float windAngle = _random.Randfn(config.windAngleAverage, config.windAngleStdDev);
        windAngle = Mathf.Wrap(windAngle, 0, Mathf.Pi * 2);
        float windAngleUpperClamp = Mathf.Wrap(config.windAngleAverage + Mathf.Pi / 4, 0, Mathf.Pi * 2);
        float windAngleLowerClamp = Mathf.Wrap(config.windAngleAverage - Mathf.Pi / 4, 0, Mathf.Pi * 2);
        windAngle = Mathf.Clamp(windAngle, windAngleLowerClamp, windAngleUpperClamp);

        // phase shift has random distribution
        float phaseShift = waveIndex * (Mathf.Pi * 2) / config.noWaves;

        return new Wave(wavelength, amplitude, windAngle, phaseShift, config.waterDepth);
    }
}

public struct ShaderSurfacePerturbationConfig {
    public Image noise;
    public float noiseScale;
    public float heightScale;
    public float timeScale;
}

// Owns the shading and dynamics of a single water tile
public partial class WaterTile : MeshInstance3D {
    // adjust noWave instead to change the number of waves
    // this must match the shader
    const int maxWaves = 10;

    // TODO: allow Ocean to abstractly configure this with an "ocean intensity"
    [Export]
    public int NoWaves {
        get {
            return _noWaves;
        }
        set {
            _noWaves = value;
            OnRebuildShaders();
        }
    }
    private int _noWaves = 10;

    [Export]
    public float WaterDepth {
        get {
            return _waterDepth;
        }
        set {
            _waterDepth = value;
            OnRebuildShaders();
        }
    }
    private float _waterDepth = 1000f;

    [Export]
    public float WindAngle {
        get {
            return _windAngle;
        }
        set {
            _windAngle = value;
            OnRebuildShaders();
        }
    }
    private float _windAngle = Mathf.Pi;

    public ShaderMaterial Material;
    public bool DebugWaves = false;

    private static ShaderSurfacePerturbationConfig surfaceConfig = new ShaderSurfacePerturbationConfig() {
        noiseScale = 10.0f,
        heightScale = 0.15f,
        timeScale = 0.025f,
    };

    WaveSet waveSet;

    public override void _Ready() {
        ConfigureShader();
    }

    public override void _Process(double delta) {
        double waveTime = GetParent<Ocean>().WaveTime;
        Material.SetShaderParameter("wave_time", waveTime);
    }

    public void OnRebuildShaders() {
        Ocean parent = GetParent<Ocean>();
        WaterDepth = parent.WaterDepth;
        ConfigureShader();
    }

    private void ConfigureShader() {
        // configure shader
        ConfigureShaderSurfacePerturbation();

        Ocean parent = GetParent<Ocean>();
        WaveSetConfig waveSetConfig = new WaveSetConfig() {
            noWaves = parent.NoWaves,
            wavelengthAverage = 8f,
            wavelengthStdDev = 1f,
            amplitudeAverage = 0.1f,
            windAngleAverage = parent.WindAngle,
            windAngleStdDev = Mathf.DegToRad(30f),
            waterDepth = WaterDepth,
        };
        waveSet = new WaveSet(waveSetConfig);
        ConfigureShaderGerstnerWaves();
    }

    private void ConfigureShaderSurfacePerturbation() {
        // water perturbations at very small level
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

        // only fill the first n waves
        // default k == 0 tells the shader to ignore the wave
        for (int i = 0; i < waveSet.waves.Count; i++) {
            amplitude[i] = waveSet.waves[i].amplitude;
            k[i] = waveSet.waves[i].k;
            kX[i] = waveSet.waves[i].kX;
            kZ[i] = waveSet.waves[i].kZ;
            omega[i] = waveSet.waves[i].angularFrequency;
            phi[i] = waveSet.waves[i].phaseShift;

            if (DebugWaves) {
                GD.Print($"Generated wave {i}:");
                GD.Print($"\tamplitude: {amplitude[i]}");
                GD.Print($"\tk: {k[i]}");
                GD.Print($"\tkX: {kX[i]}");
                GD.Print($"\tkZ: {kZ[i]}");
                GD.Print($"\tomega: {omega[i]}");
                GD.Print($"\tphi: {phi[i]}");
                GD.Print($"\tkA: {k[i] * amplitude[i]}");
                GD.Print($"\twindAngle: {waveSet.waves[i].windAngle}");
            }

            // precompute these for performance
            // these are product operands in the x/z displacement sums
            productOperandX[i] = (kX[i] / k[i]) * (amplitude[i] / Mathf.Tanh(k[i] * WaterDepth));
            productOperandZ[i] = (kZ[i] / k[i]) * (amplitude[i] / Mathf.Tanh(k[i] * WaterDepth));
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

    public float GetHeight(Vector3 worldPosition, double waveTime) {
        // TODO: update to Gerstner waves
        // this always should match the vertex shader algorithm for physics to be visually consistent
        int uvX = (int)Mathf.Wrap(worldPosition.X / surfaceConfig.noiseScale + waveTime * surfaceConfig.timeScale, 0.0, 1.0);
        int uvY = (int)Mathf.Wrap(worldPosition.Z / surfaceConfig.noiseScale + waveTime * surfaceConfig.timeScale, 0.0, 1.0);
        return GlobalPosition.Y + surfaceConfig.noise.GetPixel(uvX * surfaceConfig.noise.GetWidth(), uvY * surfaceConfig.noise.GetHeight()).R * surfaceConfig.heightScale;
    }
}

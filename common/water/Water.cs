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

	public Wave(float wavelength, float amplitude, float windAngle, float phaseShift, float waterDepth) {
		// k is the wave number (magnitude of the wave vector)
		// (kX, kZ) is the wave vector, which is the direction of the wave
		GD.Print($"Generating wave with wavelength {wavelength}, amplitude {amplitude}, wind angle {windAngle}, and water depth {waterDepth}");
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
	public float wavelengthVarianceStdDev;

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
		float wavelength = _random.Randfn(config.wavelengthAverage, config.wavelengthVarianceStdDev);
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

public partial class Water : MeshInstance3D {
	// TODO: export scene variables

	double waveTime = 0.0f;
	ShaderMaterial material;

	ShaderSurfacePerturbationConfig surfaceConfig = new ShaderSurfacePerturbationConfig() {
		noiseScale = 10.0f,
		heightScale = 0.15f,
		timeScale = 0.025f,
	};

	float waterDepth = 1000f;

	// adjust noWave instead to change the number of waves
	// this must match the shader
	const int maxWaves = 10;
	WaveSet waveSet;

	public override void _Ready() {
		material = (ShaderMaterial)Mesh.SurfaceGetMaterial(0);

		ConfigureShaderSurfacePerturbation();

		WaveSetConfig waveSetConfig = new WaveSetConfig() {
			noWaves = 4,
			wavelengthAverage = 6f,
			wavelengthVarianceStdDev = 1.0f,
			amplitudeAverage = 0.2f,
			windAngleAverage = Mathf.Pi * 0.75f,
			windAngleStdDev = Mathf.DegToRad(5f),
			waterDepth = waterDepth,
		};
		waveSet = new WaveSet(waveSetConfig);
		ConfigureShaderGerstnerWaves();
	}

	public override void _Process(double delta) {
		waveTime += delta;
		material.SetShaderParameter("wave_time", waveTime);
	}

	private void ConfigureShaderSurfacePerturbation() {
		// water perturbations at very small level
		var noiseTexture = (NoiseTexture2D)material.GetShaderParameter("wave");
		surfaceConfig.noise = noiseTexture.Noise.GetSeamlessImage(512, 512);

		material.SetShaderParameter("noise_scale", surfaceConfig.noiseScale);
		material.SetShaderParameter("height_scale", surfaceConfig.heightScale);
		material.SetShaderParameter("time_scale", surfaceConfig.timeScale);
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

#if DEBUG
			GD.Print($"Generated wave {i}:");
			GD.Print($"\tamplitude: {amplitude[i]}");
			GD.Print($"\tk: {k[i]}");
			GD.Print($"\tkX: {kX[i]}");
			GD.Print($"\tkZ: {kZ[i]}");
			GD.Print($"\tomega: {omega[i]}");
			GD.Print($"\tphi: {phi[i]}");
			GD.Print($"\tkA: {k[i] * amplitude[i]}");
#endif

			// precompute these for performance
			// these are product operands in the x/z displacement sums
			productOperandX[i] = (kX[i] / k[i]) * (amplitude[i] / Mathf.Tanh(k[i] * waterDepth));
			productOperandZ[i] = (kZ[i] / k[i]) * (amplitude[i] / Mathf.Tanh(k[i] * waterDepth));
		}

		material.SetShaderParameter("gerstner_k", k);
		material.SetShaderParameter("gerstner_k_x", kX);
		material.SetShaderParameter("gerstner_k_z", kZ);
		material.SetShaderParameter("gerstner_a", amplitude);
		material.SetShaderParameter("gerstner_omega", omega);
		material.SetShaderParameter("gerstner_phi", phi);
		material.SetShaderParameter("gerstner_product_operand_x", productOperandX);
		material.SetShaderParameter("gerstner_product_operand_z", productOperandZ);
	}

	public float GetHeight(Vector3 worldPosition) {
		// TODO: update to Gerstner waves
		// this always should match the vertex shader algorithm for physics to be visually consistent
		int uvX = (int)Mathf.Wrap(worldPosition.X / surfaceConfig.noiseScale + waveTime * surfaceConfig.timeScale, 0.0, 1.0);
		int uvY = (int)Mathf.Wrap(worldPosition.Z / surfaceConfig.noiseScale + waveTime * surfaceConfig.timeScale, 0.0, 1.0);
		return GlobalPosition.Y + surfaceConfig.noise.GetPixel(uvX * surfaceConfig.noise.GetWidth(), uvY * surfaceConfig.noise.GetHeight()).R * surfaceConfig.heightScale;
	}
}

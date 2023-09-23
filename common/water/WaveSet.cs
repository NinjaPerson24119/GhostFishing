using Godot;
using System;
using System.Collections.Generic;

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

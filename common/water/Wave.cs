using Godot;

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
        // forward prop for debugging completeness
        this.windAngle = windAngle;

        // k is the wave number (magnitude of the wave vector)
        // (kX, kZ) is the wave vector, which is the direction of the wave
        kX = 2 * Mathf.Pi / wavelength * Mathf.Cos(windAngle);
        kZ = 2 * Mathf.Pi / wavelength * Mathf.Sin(windAngle);
        k = Mathf.Sqrt(kX * kX + kZ * kZ);
        DebugTools.Assert(k * amplitude < 1, $"Wave function does not satisfy kA < 1. k: {k}, a: {amplitude}. Max amplitude for this wave is {1 / k}");

        this.amplitude = amplitude;
        angularFrequency = Mathf.Sqrt(gravity * k * Mathf.Tanh(k * waterDepth));
        this.phaseShift = phaseShift;
    }
}
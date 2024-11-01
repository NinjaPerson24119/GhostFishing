using Godot;

// need residual intensities after end of period, but sun and moon do not overlap

internal partial class PlanetaryLight : DirectionalLight3D {
    // these are controlled by GameEnvironment (hence the lack of an Export attribute)
    // start of period relative to module time (24hrs clock)
    public float StartSecondsModuloTime;
    // how long to keep the light on for after the start
    public float DurationSeconds;

    [Export]
    public float LightEnergyAtSolarNoon = 1f;

    /*
     * X axis is East-West
     *
     * Sun / Moon rises in the East and sets in the West
     * Point 0, -90, -180 for East, Midday, West respectively
    */
    [Export]
    public float LightAngleAtRise = 0;
    [Export]
    public float LightAngleAtSet = -Mathf.Pi;

    [Export]
    public bool DebugLogs = false;

    [Signal]
    public delegate void PlanetaryLightChangedEventHandler(float cycleProgression, float elevationFactor, float lightEnergy);

    public float CycleProgression { get; private set; } = -1;
    public float ElevationFactor { get; private set; }
    public float LightAngle { get; private set; }

    // Wait for GameEnvironment to configuration to be set
    public bool Started { get; private set; } = false;
    public void Start() {
        Started = true;
        DebugTools.Assert(DurationSeconds < GameClock.SecondsPerDay, "DurationSeconds must be less than a day");
        GD.Print($"{Name} Cycle - Start Hour: {StartSecondsModuloTime / GameClock.SecondsPerHour}, Duration Hours: {DurationSeconds / GameClock.SecondsPerHour}");
    }

    public void Update(double gameSeconds) {
        if (!Started) {
            return;
        }

        UpdateCycleProgression((float)gameSeconds);
        Visible = CycleProgression != -1;
        if (Visible) {
            UpdateElevationFactor(CycleProgression);
            UpdateLightAngle(CycleProgression, ElevationFactor);
            UpdateIntensity(ElevationFactor);

            if (DebugLogs) {
                GD.Print($"{Name} - Cycle: {CycleProgression}, ElevationFactor: {ElevationFactor}, LightAngle: {Mathf.RadToDeg(LightAngle)}, LightEnergy: {LightEnergy}");
            }
            EmitSignal(SignalName.PlanetaryLightChanged, CycleProgression, ElevationFactor, LightEnergy);
        }
    }

    // returns progression factor [0,1] through cycle, or -1 if not in cycle
    private void UpdateCycleProgression(float gameSeconds) {
        ScheduledEvent e = new ScheduledEvent(StartSecondsModuloTime, DurationSeconds, GameClock.SecondsPerDay);
        CycleProgression = e.GetProgression(gameSeconds);
    }

    private void UpdateElevationFactor(float cycleProgression) {
        // model elevation curve with simple sine
        ElevationFactor = Mathf.Sin(Mathf.Pi * cycleProgression);
    }

    private void UpdateLightAngle(float cycleProgression, float elevationFactor) {
        // weighted average to solar noon, with elevation factor as weight
        // don't want to directly calculate elevation angle as function of cycle time since it's not linear
        float solarNoonAngle = (LightAngleAtRise + LightAngleAtSet) / 2;
        if (cycleProgression < 0.5) {
            // before midday
            LightAngle = LightAngle = LightAngleAtRise * (1 - elevationFactor) + solarNoonAngle * elevationFactor;
        }
        else {
            // after midday
            LightAngle = LightAngleAtSet * (1 - elevationFactor) + solarNoonAngle * elevationFactor;
        }
        Rotation = new Vector3(LightAngle, 0, 0);
    }

    private void UpdateIntensity(float elevationFactor) {
        // taper intensity around midday and sunrise/sunset
        // steepness <10 will not actually achieve 100% intensity, but that's sometimes fine
        LightEnergy = Sigmoid(elevationFactor, 0.5f, 10f) * LightEnergyAtSolarNoon;
    }

    public static float Sigmoid(float x, float midpoint, float steepness) {
        return 1 / (1 + Mathf.Exp(-steepness * (x - midpoint)));
    }
}

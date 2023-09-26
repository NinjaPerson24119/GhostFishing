using Godot;

// need residual intensities after end of period, but sun and moon do not overlap

public partial class DayNightCycle : DirectionalLight3D {
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

    [Signal]
    public delegate void CycleProgressionChangedEventHandler(float cycleProgression);

    public float CycleProgression { get; private set; }
    public float ElevationFactor { get; private set; }
    public float LightAngle { get; private set; }

    // Wait for GameEnvironment to configuration to be set
    public bool Started { get; private set; } = false;
    public void Start() {
        Started = true;
        DebugTools.Assert(DurationSeconds < GameEnvironment.SECONDS_PER_DAY, "DurationSeconds must be less than a day");
        GD.Print($"{Name} Cycle - Start Hour: {StartSecondsModuloTime / GameEnvironment.SECONDS_PER_HOUR}, Duration Hours: {DurationSeconds / GameEnvironment.SECONDS_PER_HOUR}");
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

            GD.Print($"{Name} - Cycle: {CycleProgression}, ElevationFactor: {ElevationFactor}, LightAngle: {Mathf.RadToDeg(LightAngle)}, LightEnergy: {LightEnergy}");
        }
    }

    // returns progression factor [0,1] through cycle, or -1 if not in cycle
    private void UpdateCycleProgression(float gameSeconds) {
        //7 => 0 * 24 + 7, so sub one set for moon on first run
        //9 => 0 * 24 + 8, and 9 > 8, so no sub
        float startOfLastCycle = Mathf.Floor(gameSeconds / GameEnvironment.SECONDS_PER_DAY) * GameEnvironment.SECONDS_PER_DAY + StartSecondsModuloTime;
        if (startOfLastCycle > gameSeconds) {
            // we are in the first cycle
            startOfLastCycle -= GameEnvironment.SECONDS_PER_DAY;
        }

        //GD.Print($"Shifted game second hours: {gameSeconds / GameEnvironment.SECONDS_PER_HOUR}");
        //GD.Print($"{Name} - Start of last cycle: {startOfLastCycle / GameEnvironment.SECONDS_PER_HOUR}");
        float secondsIntoCycle = gameSeconds - startOfLastCycle;
        //GD.Print($"{Name} - Hours into cycle: {gameSeconds / GameEnvironment.SECONDS_PER_HOUR} - {startOfLastCycle / GameEnvironment.SECONDS_PER_HOUR} = {secondsIntoCycle / GameEnvironment.SECONDS_PER_HOUR}");
        if (secondsIntoCycle > DurationSeconds) {
            //GD.Print($"Skip {Name}: {secondsIntoCycle / GameEnvironment.SECONDS_PER_HOUR} > {DurationSeconds / GameEnvironment.SECONDS_PER_HOUR}");
            CycleProgression = -1;
            return;
        }

        CycleProgression = secondsIntoCycle / DurationSeconds;
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
            LightAngle = LightAngleAtRise * (1 - elevationFactor) + solarNoonAngle * elevationFactor;
        }

        // TODO: updating rotation breaks light
        //Rotation = new Vector3(LightAngle, 0, 0);
        Rotation = new Vector3(-Mathf.Pi / 2, 0, 0);
    }

    private void UpdateIntensity(float elevationFactor) {
        // taper intensity around midday and sunrise/sunset
        LightEnergy = Sigmoid(elevationFactor, 0.5f, 10f) * LightEnergyAtSolarNoon;
    }

    public static float Sigmoid(float x, float midpoint, float steepness) {
        return 1 / (1 + Mathf.Exp(-steepness * (x - midpoint)));
    }
}

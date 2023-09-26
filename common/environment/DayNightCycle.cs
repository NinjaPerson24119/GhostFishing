using Godot;

// need residual intensities after end of period, but sun and moon do not overlap

public partial class DayNightCycle : DirectionalLight3D {
    [Export]
    public float StartSeconds;
    [Export]
    public float EndSeconds;
    // If true, the cycle period is considered the outside of the (start, end) interval.
    private bool _useExteriorOfWindow = false;
    private float _cyclePeriodSeconds;

    [Export]
    public float IntensityAtSolarNoon = 1f;

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

    public float CycleProgression { get; private set; }
    public float ElevationFactor { get; private set; }
    public float LightAngle { get; private set; }
    public float Intensity { get; private set; }

    public override void _Ready() {
        // if the period would cross the end of day, we need to use the exterior of the window
        _useExteriorOfWindow = StartSeconds > EndSeconds;

        float cyclePeriodInsideOfWindow = Mathf.Abs(StartSeconds - EndSeconds);
        if (_useExteriorOfWindow) {
            // swap bounds
            float temp = StartSeconds;
            StartSeconds = EndSeconds;
            EndSeconds = temp;

            // calculate period outside of window
            _cyclePeriodSeconds = GameEnvironment.SECONDS_PER_HOUR * 24 - cyclePeriodInsideOfWindow;
        }
        else {
            _cyclePeriodSeconds = cyclePeriodInsideOfWindow;
        }
        GD.Print($"{Name} cycle: {_cyclePeriodSeconds / GameEnvironment.SECONDS_PER_HOUR} hours");
    }

    public void Update(double gameSecondsToday) {
        UpdateCycleProgression((float)gameSecondsToday);
        Visible = CycleProgression != -1;
        if (Visible) {
            UpdateElevationFactor(CycleProgression);
            UpdateLightAngle(CycleProgression, ElevationFactor);
            UpdateIntensity(ElevationFactor);

            GD.Print($"{Name} - Cycle: {CycleProgression}, ElevationFactor: {ElevationFactor}, LightAngle: {LightAngle}, Intensity: {Intensity}");
        }
    }

    // returns progression factor [0,1] through cycle, or -1 if not in cycle
    private void UpdateCycleProgression(float gameSecondsToday) {
        bool inWindow = gameSecondsToday >= StartSeconds && gameSecondsToday <= EndSeconds;
        if (!_useExteriorOfWindow) {
            inWindow = !inWindow;
        }

        if (!inWindow) {
            CycleProgression = -1;
            return;
        }
        float secondsIntoCycle = gameSecondsToday - StartSeconds;
        CycleProgression = secondsIntoCycle / _cyclePeriodSeconds;
    }

    private void UpdateElevationFactor(float cycleProgression) {
        // model elevation curve with simple sine
        ElevationFactor = Mathf.Sin(Mathf.Pi * cycleProgression);
    }

    private void UpdateLightAngle(float cycleProgression, float elevationFactor) {
        float halfPeriod = _cyclePeriodSeconds / 2;
        // angle is step function dependent on whether the cycle is before or after midday
        if (cycleProgression < 0.5) {
            // before midday
            LightAngle = LightAngleAtRise + halfPeriod * elevationFactor;
        }
        else {
            // after midday
            LightAngle = LightAngleAtSet - halfPeriod * elevationFactor;
        }
        Rotation = new Vector3(LightAngle, 0, 0);
    }

    private void UpdateIntensity(float elevationFactor) {
        Intensity = elevationFactor * IntensityAtSolarNoon;
        // taper intensity around midday and sunrise/sunset
        LightEnergy = Sigmoid(Intensity, 0.5f, 10f);
    }

    public static float Sigmoid(float x, float midpoint, float steepness) {
        return 1 / (1 + Mathf.Exp(-steepness * (x - midpoint)));
    }
}

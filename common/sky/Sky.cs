using Godot;

internal partial class Sky : Node3D {
	private const string _sunPathLight = "SunLight";
	//private const string _moonPathLight = "MoonLight";
	private const string _worldEnvironmentPath = "WorldEnvironment";

	private Environment _environment = null!;

	[Export]
	public float SecondsAtSunriseModuloTime = 8 * GameClock.SecondsPerHour;

	[Export]
	public float DayTimeSkyMaxLightEnergy = 0.4f;
	[Export]
	public float DayTimeMinLightEnergy = 0.2f;
	[Export]
	public float NightTimeSkyMaxLightEnergy = 0.2f;
	[Export]
	public float NightTimeMinLightEnergy = 0.1f;
	[Export]
	public Color SunLightColor {
		get {
			return _sunLightColor;
		}
		set {
			_sunLightColor = value;
			GetNode<PlanetaryLight>(_sunPathLight).LightColor = value;
		}
	}
	private Color _sunLightColor = Color.FromString("#ffffee", Colors.White);
	[Export]
	public Color MoonLightColor {
		get {
			return _moonLightColor;
		}
		set {
			_moonLightColor = value;
			//GetNode<PlanetaryLight>(_moonPathLight).LightColor = value;
		}
	}
	private Color _moonLightColor = Color.FromString("#c3ffff", Colors.White);

	public override void _Ready() {
		_environment = GetNode<WorldEnvironment>(_worldEnvironmentPath).Environment;

		SetLightCycles();
		GameClock.ConnectGameSecondsChanged(GetNode<PlanetaryLight>(_sunPathLight).Update);
		//GameClock.ConnectGameSecondsChanged(GetNode<PlanetaryLight>(_moonPathLight).Update);
		GetNode<PlanetaryLight>(_sunPathLight).PlanetaryLightChanged += OnSunChanged;
		//GetNode<PlanetaryLight>(_moonPathLight).PlanetaryLightChanged += OnMoonChanged;
	}

	private void SetLightCycles() {
		PlanetaryLight sun = GetNode<PlanetaryLight>(_sunPathLight);
		sun.StartSecondsModuloTime = SecondsAtSunriseModuloTime;
		sun.DurationSeconds = GameClock.SecondsPerDay / 2f;
		sun.Start();

		//PlanetaryLight moon = GetNode<PlanetaryLight>(_moonPathLight);
		//moon.StartSecondsModuloTime = sun.StartSecondsModuloTime + sun.DurationSeconds;
		//moon.DurationSeconds = GameClock.SecondsPerDay / 2f;
		//moon.Start();
	}

	// Set sky as constant multiple of sun/moon energy so it doesn't appear disproportionately bright/dark
	public void OnSunChanged(float cycleProgression, float elevationFactor, float lightEnergy) {
		if (cycleProgression != -1) {
			_environment.BackgroundColor = _sunLightColor;
			_environment.BackgroundEnergyMultiplier = lightEnergy * 0.1f;
		}
	}

	public void OnMoonChanged(float cycleProgression, float elevationFactor, float lightEnergy) {
		if (cycleProgression != -1) {
			_environment.BackgroundColor = _sunLightColor;
			_environment.BackgroundEnergyMultiplier = lightEnergy * 0.1f;
			//BackgroundEnergyMultiplier = Mathf.Max((1 - elevationFactor) * NightTimeSkyMaxLightEnergy, NightTimeMinLightEnergy);
		}
	}
}

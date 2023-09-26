using Godot;

public partial class Sky : WorldEnvironment {
	private const string _sunPath = "Sun";
	private const string _moonPath = "Moon";
	private float _sunCycle;
	private float _moonCycle;

	[Export]
	public float SecondsAtSunriseModuloTime = 8 * GameClock.SecondsPerHour;

	public override void _Ready() {
		SetLightCycles();

		GameClock.ConnectGameSecondsChanged(GetNode<PlanetaryLight>(_sunPath).Update);
		GameClock.ConnectGameSecondsChanged(GetNode<PlanetaryLight>(_moonPath).Update);
		GetNode<PlanetaryLight>(_sunPath).CycleProgressionChanged += UpdateSunCycle;
		GetNode<PlanetaryLight>(_moonPath).CycleProgressionChanged += UpdateMoonCycle;
	}

	public override void _Process(double delta) {
		GD.Print($"Sun: {_sunCycle}, Moon: {_moonCycle}");

	}

	public void UpdateSunCycle(float cycleProgression) {
		_sunCycle = cycleProgression;
	}

	public void UpdateMoonCycle(float cycleProgression) {
		_moonCycle = cycleProgression;
	}

	private void SetLightCycles() {
		PlanetaryLight sun = GetNode<PlanetaryLight>(_sunPath);
		sun.StartSecondsModuloTime = SecondsAtSunriseModuloTime;
		sun.DurationSeconds = GameClock.SecondsPerDay / 2f;
		sun.Start();

		PlanetaryLight moon = GetNode<PlanetaryLight>(_moonPath);
		moon.StartSecondsModuloTime = sun.StartSecondsModuloTime + sun.DurationSeconds;
		moon.DurationSeconds = GameClock.SecondsPerDay / 2f;
		moon.Start();
	}
}

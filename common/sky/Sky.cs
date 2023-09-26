using Godot;

public partial class Sky : Node3D {
	private const string _sunPathLight = "SunLight";
	private const string _moonPathLight = "MoonLight";
	private float _sunCycle;
	private float _moonCycle;

	[Export]
	public float SecondsAtSunriseModuloTime = 8 * GameClock.SecondsPerHour;

	public override void _Ready() {
		SetLightCycles();

		GameClock.ConnectGameSecondsChanged(GetNode<PlanetaryLight>(_sunPathLight).Update);
		GameClock.ConnectGameSecondsChanged(GetNode<PlanetaryLight>(_moonPathLight).Update);
		GetNode<PlanetaryLight>(_sunPathLight).CycleProgressionChanged += UpdateSunCycle;
		GetNode<PlanetaryLight>(_moonPathLight).CycleProgressionChanged += UpdateMoonCycle;
	}

	public override void _Process(double delta) {
		//GD.Print($"Sun: {_sunCycle}, Moon: {_moonCycle}");
	}

	public void UpdateSunCycle(float cycleProgression) {
		_sunCycle = cycleProgression;
	}

	public void UpdateMoonCycle(float cycleProgression) {
		_moonCycle = cycleProgression;
	}

	private void SetLightCycles() {
		PlanetaryLight sun = GetNode<PlanetaryLight>(_sunPathLight);
		sun.StartSecondsModuloTime = SecondsAtSunriseModuloTime;
		sun.DurationSeconds = GameClock.SecondsPerDay / 2f;
		sun.Start();

		PlanetaryLight moon = GetNode<PlanetaryLight>(_moonPathLight);
		moon.StartSecondsModuloTime = sun.StartSecondsModuloTime + sun.DurationSeconds;
		moon.DurationSeconds = GameClock.SecondsPerDay / 2f;
		moon.Start();
	}
}

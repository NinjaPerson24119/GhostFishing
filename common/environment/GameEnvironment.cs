using Godot;

public partial class GameEnvironment : Node3D {
	public const int SECONDS_PER_HOUR = 60 * 60;
	public const float SECONDS_PER_DAY = 24 * SECONDS_PER_HOUR;

	[Export]
	// 60 minutes per second
	public double GameSecondsPerRealSecond { get; private set; } = 60 * 60f;

	[Export]
	private double GameSeconds {
		get {
			return _gameSeconds;
		}
		set {
			_gameSeconds = value;
		}
	}
	// start game with sun rising
	private double _gameSeconds = 7.5 * SECONDS_PER_HOUR;

	[Export]
	public float SecondsAtSunriseModuloTime = 8 * SECONDS_PER_HOUR;

	[Export]
	public float WindAngle = 0f;
	[Export]
	public float WindAngleChangePerHour = 10f;

	[Signal]
	public delegate void GameSecondsChangedEventHandler(double gameSeconds);

	private RandomNumberGenerator _random = new RandomNumberGenerator();

	public override void _Ready() {
		SetLightCycles();

		GameSecondsChanged += GetNode<DayNightCycle>("Sun").Update;
		GameSecondsChanged += GetNode<DayNightCycle>("Moon").Update;
		GetNode<DayNightCycle>("Sun").CycleProgressionChanged += GetNode<WorldEnvironmentInstance>("WorldEnvironmentInstance").UpdateSunCycle;
		GetNode<DayNightCycle>("Moon").CycleProgressionChanged += GetNode<WorldEnvironmentInstance>("WorldEnvironmentInstance").UpdateMoonCycle;
	}

	public override void _Process(double delta) {
		// do not rely on RealClock.RealTime because updating the timescale could cause us to time travel to the past
		GameSeconds += delta * GameSecondsPerRealSecond;
		EmitSignal(SignalName.GameSecondsChanged, GameSeconds);

		UpdateWind(delta);
	}

	private void SetLightCycles() {
		DayNightCycle sun = GetNode<DayNightCycle>("Sun");
		sun.StartSecondsModuloTime = SecondsAtSunriseModuloTime;
		sun.DurationSeconds = SECONDS_PER_DAY / 2f;
		//sun.Start();

		DayNightCycle moon = GetNode<DayNightCycle>("Moon");
		moon.StartSecondsModuloTime = sun.StartSecondsModuloTime + sun.DurationSeconds;
		moon.DurationSeconds = SECONDS_PER_DAY / 2f;
		//moon.Start();
	}

	private void UpdateWind(double delta) {
		float windAngleChangePerSecond = WindAngleChangePerHour / SECONDS_PER_HOUR;
		float direction = _random.Randi() % 2 == 0 ? 1 : -1;
		WindAngle += direction * windAngleChangePerSecond * (float)delta;

		// TODO
		// Ocean will need to support gradual parameter updates
		//Ocean ocean = GetNode<Ocean>("Ocean");
	}
}

using Godot;

public partial class GameEnvironment : Node3D {
	public const int SECONDS_PER_HOUR = 60 * 60;
	public const float SECONDS_PER_DAY = 24 * SECONDS_PER_HOUR;

	[Export]
	// 10 minutes per second
	public double GameSecondsPerRealSecond { get; private set; } = 10 * 60f;

	[Export]
	private double GameSecondsToday {
		get {
			return _gameSecondsToday;
		}
		set {
			_gameSecondsToday = value;
		}
	}
	private double _gameSecondsToday = 7.5 * SECONDS_PER_HOUR;

	[Export]
	public float SecondsAtSunrise = 8 * SECONDS_PER_HOUR;

	[Export]
	public float WindAngle = 0f;
	[Export]
	public float WindAngleChangePerHour = 10f;

	[Signal]
	public delegate void GameSecondsTodayChangedEventHandler(double gameSecondsToday);

	private RandomNumberGenerator _random = new RandomNumberGenerator();

	public override void _Ready() {
		SetLightCycles();

		GameSecondsTodayChanged += GetNode<DayNightCycle>("Sun").Update;
        GameSecondsTodayChanged += GetNode<DayNightCycle>("Moon").Update;
	}

	public override void _Process(double delta) {
		// do not rely on RealClock.RealTime because updating the timescale could cause us to go backwards
		GameSecondsToday += delta * GameSecondsPerRealSecond;
		GameSecondsToday = GameSecondsToday % SECONDS_PER_DAY;
		EmitSignal(SignalName.GameSecondsTodayChanged, GameSecondsToday);

		UpdateWind(delta);
	}

	private void SetLightCycles() {
		DayNightCycle sun = GetNode<DayNightCycle>("Sun");
		sun.StartSeconds = SecondsAtSunrise;
		sun.EndSeconds = SecondsAtSunrise + SecondsAtSunrise;

		DayNightCycle moon = GetNode<DayNightCycle>("Moon");
		moon.StartSeconds = sun.EndSeconds;
		moon.EndSeconds = sun.StartSeconds;
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

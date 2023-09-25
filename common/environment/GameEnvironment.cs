using Godot;

public partial class GameEnvironment : Node3D {
	public const int SECONDS_IN_HOUR = 60 * 60;
	public const float SECONDS_PER_DAY = 24 * SECONDS_IN_HOUR;

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
	private double _gameSecondsToday = 6 * SECONDS_IN_HOUR;

	[Export]
	public float secondsAtSunrise = 8 * SECONDS_IN_HOUR;

	[Export]
	public float WindAngle = 0f;
	[Export]
	public float WindAngleChangePerHour = 10f;

	[Signal]
	public delegate void GameSecondsTodayChangedEventHandler(double gameSecondsToday);

	private RandomNumberGenerator _random = new RandomNumberGenerator();

	public override void _Process(double delta) {
		// do not rely on RealClock.RealTime because updating the timescale could cause us to go backwards
		GameSecondsToday += delta * GameSecondsPerRealSecond;
		GameSecondsToday = GameSecondsToday % SECONDS_PER_DAY;
		EmitSignal(SignalName.GameSecondsTodayChanged, GameSecondsToday);

		UpdateSunAndMoon();
		UpdateWind(delta);
	}

	public void UpdateSunAndMoon() {
		float dayNightFactor = DayNightFactor(GameSecondsToday, SECONDS_PER_DAY, secondsAtSunrise);

		// sun rises in the East and sets in the West
		// point 0, -90, -180 for East, Midday, West respectively
		DirectionalLight3D sun = GetNode<DirectionalLight3D>("Sun");
		float sunFactor = Mathf.Clamp(dayNightFactor, 0, 1);
		sun.Rotation = new Vector3(0, 0, (1 - sunFactor) * -180);

		// moon rises in the West and sets in the East
		// point 0, 90, 180 for West, Midnight, East respectively
		DirectionalLight3D moon = GetNode<DirectionalLight3D>("Moon");
		float moonFactor = Mathf.Clamp(dayNightFactor, -1, 0);
		moon.Rotation = new Vector3(0, 0, moonFactor * 180);

		sun.Visible = dayNightFactor >= 0;
		moon.Visible = dayNightFactor < 0;
	}

	private float DayNightFactor(double time, float period, float sunrise) {
		float scale = 2 * Mathf.Pi / period;
		float translation = Mathf.Pi / 2 - 2 * Mathf.Pi * sunrise / period;
		return (float)-Mathf.Cos(scale * time + translation);
	}

	private void UpdateWind(double delta) {
		float windAngleChangePerSecond = WindAngleChangePerHour / SECONDS_IN_HOUR;
		float direction = _random.Randi() % 2 == 0 ? 1 : -1;
		WindAngle += direction * windAngleChangePerSecond * (float)delta;

		// TODO
		// Ocean will need to support gradual parameter updates
		//Ocean ocean = GetNode<Ocean>("Ocean");
	}
}

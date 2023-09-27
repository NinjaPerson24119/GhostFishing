using Godot;

// cannot export static variables, so we use private variables and a singleton accessor pattern
public partial class GameClock : Node {
    static SingletonTracker<GameClock> _singletonTracker = new SingletonTracker<GameClock>();
    private static GameClock _singleton { get => _singletonTracker.Ref(); }

    public static float SecondsPerHour = 60 * 60;
    public static float SecondsPerDay = 24 * SecondsPerHour;

    [Export]
    // 10 minutes per second
    private double _gameSecondsPerRealSecond = 60 * 60f;
    public static double GameSecondsPerRealSecond { get => _singleton._gameSecondsPerRealSecond; }

    [Export]
    // start game with sun rising
    private double _gameSeconds = 10 * SecondsPerHour;
    public static double GameSeconds { get => _singleton._gameSeconds; }

    // DEBUG
    private bool _paused = true;
    public static bool Paused { get => _singleton._paused; }
    public static void TogglePause() {
        _singleton._paused = !_singleton._paused;
    }

    // cannot return signal from function, so we need action functions like Connect/Disconnect
    [Signal]
    public delegate void _gameSecondsChangedEventHandler(double gameSeconds);
    public static void ConnectGameSecondsChanged(_gameSecondsChangedEventHandler f) {
        _singleton._gameSecondsChanged += f;
    }

    public override void _Ready() {
        _singletonTracker.Ready(this);
    }

    public override void _Process(double delta) {
        // do not rely on RealClock.RealTime because updating the timescale could cause us to time travel to the past
        if (!_singleton._paused) {
            _gameSeconds += delta * _gameSecondsPerRealSecond;
        }
        EmitSignal(SignalName._gameSecondsChanged, _gameSeconds);
    }
}

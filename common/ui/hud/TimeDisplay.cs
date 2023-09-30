using Godot;

public partial class TimeDisplay : Label {
    public void Update(double gameSeconds) {
        float gameSecondsToday = (float)gameSeconds % GameClock.SecondsPerDay;
        int hours = (int)Mathf.Floor(gameSecondsToday / GameClock.SecondsPerHour);
        int minutes = (int)Mathf.Floor(gameSecondsToday % GameClock.SecondsPerHour / 60);
        Text = $"{hours:D2}:{minutes:D2}";
    }
}

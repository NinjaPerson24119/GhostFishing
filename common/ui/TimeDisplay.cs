using Godot;

public partial class TimeDisplay : Label {
    public void UpdateGameSecondsToday(double gameSecondsToday) {
        int hours = (int)Mathf.Floor(gameSecondsToday / GameEnvironment.SECONDS_IN_HOUR);
        int minutes = (int)Mathf.Floor(gameSecondsToday % GameEnvironment.SECONDS_IN_HOUR / 60);
        Text = $"{hours:D2}:{minutes:D2}";
    }
}

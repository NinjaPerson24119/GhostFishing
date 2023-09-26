using Godot;

public partial class TimeDisplay : Label {
    public void Update(double gameSeconds) {
        float gameSecondsToday = (float)gameSeconds % GameEnvironment.SECONDS_PER_DAY;
        int hours = (int)Mathf.Floor(gameSecondsToday / GameEnvironment.SECONDS_PER_HOUR);
        int minutes = (int)Mathf.Floor(gameSecondsToday % GameEnvironment.SECONDS_PER_HOUR / 60);
        Text = $"{hours:D2}:{minutes:D2}";
    }
}

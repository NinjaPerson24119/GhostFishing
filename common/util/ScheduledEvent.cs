using Godot;

// Helper for mapping GameClock time to a periodic event
// E.g. day/night cycle, whether an encounter is active during a time of day, etc.
internal struct ScheduledEvent {
    public float StartSecondsModuloTime;
    public float DurationSeconds;
    public float ModuloTimeSeconds;

    public ScheduledEvent(float startSecondsModuloTime, float durationSeconds, float moduloTimeSeconds) {
        StartSecondsModuloTime = startSecondsModuloTime;
        DurationSeconds = durationSeconds;
        ModuloTimeSeconds = moduloTimeSeconds;
    }

    public float GetProgression(float gameSeconds) {
        float startOfLastCycle = Mathf.Floor(gameSeconds / ModuloTimeSeconds) * ModuloTimeSeconds + StartSecondsModuloTime;
        if (startOfLastCycle > gameSeconds) {
            // we are in the first cycle
            startOfLastCycle -= ModuloTimeSeconds;
        }

        float secondsIntoCycle = gameSeconds - startOfLastCycle;
        if (secondsIntoCycle > DurationSeconds) {
            return -1;
        }

        return secondsIntoCycle / DurationSeconds;
    }
}

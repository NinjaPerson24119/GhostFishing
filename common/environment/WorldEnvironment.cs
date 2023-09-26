using Godot;

public partial class WorldEnvironmentInstance : WorldEnvironment {
    private float _sunCycle;
    private float _moonCycle;

    public override void _Process(double delta) {

    }

    public void UpdateSunCycle(float cycleProgression) {
        _sunCycle = cycleProgression;
    }

    public void UpdateMoonCycle(float cycleProgression) {
        _moonCycle = cycleProgression;
    }
}

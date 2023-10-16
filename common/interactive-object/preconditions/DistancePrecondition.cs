using Godot;

internal class DistancePrecondition : IInteractiveObjectPrecondition {
    private float _maxDistanceSquared;

    public DistancePrecondition(float maxDistance) {
        _maxDistanceSquared = Mathf.Pow(maxDistance, 2);
    }

    public bool Check(InteractiveObject interactiveObject, Player player) {
        return TrackerDistance.DistanceSquared(player.TrackingPosition, interactiveObject.TrackingPosition) < _maxDistanceSquared;
    }
}

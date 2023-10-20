using Godot;

internal class Tracker {
    public const float DefaultDistanceSignificanceEpsilon = 0.1f;

    private float _distanceSignificanceEpsilonSquared;
    private TrackingServer _server;
    private ITrackableObject _trackableObject;
    private Vector3 _lastEmittedPosition = Vector3.Zero;

    public Tracker(ITrackableObject trackableObject, TrackingServer server, float distanceSignificanceEpsilon = DefaultDistanceSignificanceEpsilon) {
        _trackableObject = trackableObject;
        _server = server;
        _distanceSignificanceEpsilonSquared = Mathf.Pow(distanceSignificanceEpsilon, 2);

        Update();
    }

    ~Tracker() {
        _server.Remove(_trackableObject);
    }

    public void Update() {
        if (TrackerDistance.DistanceSquared(_trackableObject.TrackingPosition, _lastEmittedPosition) > _distanceSignificanceEpsilonSquared) {
            _lastEmittedPosition = _trackableObject.TrackingPosition;
            _server.Update(_trackableObject);
        }
    }
}

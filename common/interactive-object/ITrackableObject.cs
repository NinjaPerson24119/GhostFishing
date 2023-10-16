using Godot;

public interface ITrackableObject {
    public string ID { get; }
    public Vector3 TrackingPosition { get; }
}

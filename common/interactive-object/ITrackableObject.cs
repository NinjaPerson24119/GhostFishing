using Godot;

public interface ITrackableObject {
    public string TrackingID { get; }
    public Vector3 TrackingPosition { get; }
}

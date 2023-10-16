using Godot;

internal interface IInteractiveObject {
    public IInteractiveObjectAction Action { get; }
    public Vector3 TrackingPosition { get; }
    private Tracker _tracker { get; set; }
}

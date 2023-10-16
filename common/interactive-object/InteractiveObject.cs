using Godot;

public partial class InteractiveObject : ITrackableObject {
    [Export]
    public string ID {
        get {
            if (_ID == null) {
                throw new System.InvalidOperationException("ID must be set before it is accessed");
            }
            return _ID;
        }
        private set {
            if (_ID != null) {
                throw new System.InvalidOperationException("ID cannot be set more than once");
            }
            if (string.IsNullOrEmpty(value)) {
                throw new System.ArgumentException("ID cannot be null or empty");
            }
            _ID = value;
        }
    }
    private string? _ID;

    public Vector3 TrackingPosition {
        get {
            return GlobalPosition;
        }
    }

    private Tracker? _tracker;

    protected IInteractiveObjectAction? Action { get; set; }

    public InteractiveObject(Node3D attachedTo, IInteractiveObjectAction action) {
        if (action == null) {
            throw new System.ArgumentNullException("Action must be set before _Ready");
        }
        if (string.IsNullOrEmpty(ID)) {
            ID = GetPath();
        }

        TrackingServer trackingServer = DependencyInjector.Ref().GetTrackingServer();
        _tracker = new Tracker(this, trackingServer);
    }

    public void PhysicsProcess(double delta) {
        if (_tracker == null) {
            throw new System.Exception("_tracker must be set before _Process");
        }
        _tracker.Update();
    }
}

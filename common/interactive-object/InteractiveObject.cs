using Godot;

public partial class InteractiveObject : ITrackableObject {
    [Export]
    public string ID {
        get {
            return _ID;
        }
    }
    private string _ID;

    public Vector3 TrackingPosition {
        get {
            return _attachedNode.GlobalPosition;
        }
    }

    private Tracker _tracker;
    private Node3D _attachedNode;
    protected IInteractiveObjectAction Action { get; set; }

    public InteractiveObject(string id, Node3D attachedNode, IInteractiveObjectAction action) {
        if (action == null) {
            throw new System.ArgumentNullException("Action must be set before _Ready");
        }
        Action = action;

        if (string.IsNullOrEmpty(id)) {
            throw new System.ArgumentException("ID cannot be null or empty");
        }
        _ID = id;

        TrackingServer trackingServer = DependencyInjector.Ref().GetTrackingServer();
        _tracker = new Tracker(this, trackingServer);
        _attachedNode = attachedNode;
    }

    public void PhysicsProcess(double delta) {
        if (_tracker == null) {
            throw new System.Exception("_tracker must be set before _Process");
        }
        _tracker.Update();
    }
}

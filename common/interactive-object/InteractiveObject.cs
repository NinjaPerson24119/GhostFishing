using Godot;

public partial class InteractiveObject : ITrackableObject {
    [Export]
    public string TrackingID {
        get {
            return _ID;
        }
    }
    private string _ID;

    public Vector3 TrackingPosition {
        get {
            if (_attachedNode == null) {
                throw new System.Exception("_attachedNode should have been set by construction");
            }
            return _attachedNode.GlobalPosition;
        }
    }

    public InteractiveObjectAction Action { get; private set; }

    private Tracker _tracker;
    private Node3D? _attachedNode;

    public InteractiveObject(string id, Node3D attachedNode, InteractiveObjectAction action) {
        if (action == null) {
            throw new System.ArgumentNullException("Action must be set before _Ready");
        }
        Action = action;

        if (string.IsNullOrEmpty(id)) {
            throw new System.ArgumentException("ID cannot be null or empty");
        }
        _ID = id;

        _attachedNode = attachedNode;
        TrackingServer trackingServer = DependencyInjector.Ref().GetTrackingServer();
        _tracker = new Tracker(this, trackingServer);
    }

    public void PhysicsProcess(double delta) {
        if (_tracker == null) {
            throw new System.Exception("_tracker must be set before PhysicsProcess");
        }
        _tracker.Update();
    }

    public bool CheckPreconditions(Player player) {
        return Action.CheckPreconditions(this, player);
    }

    public bool Activate(Player player) {
        return Action.Activate(this, player);
    }
}

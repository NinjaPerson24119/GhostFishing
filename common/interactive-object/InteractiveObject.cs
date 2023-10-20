using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class InteractiveObject : Node3D, ITrackableObject {
    [Export]
    public string TrackingID {
        get {
            if (string.IsNullOrEmpty(_trackingID)) {
                throw new System.Exception("TrackingID must be set before it can be read");
            }
            return _trackingID;
        }
        set {
            if (_trackingID != "") {
                throw new System.Exception("TrackingID cannot be set more than once");
            }
            _trackingID = value;
        }
    }
    private string _trackingID = "";

    public Vector3 TrackingPosition {
        get => GlobalPosition;
    }

    [Export]
    public string Description { get; set; } = "";

    private List<InteractiveObjectAction> Actions = new List<InteractiveObjectAction>();
    private Tracker? _tracker;

    public override void _Ready() {
        if (string.IsNullOrEmpty(TrackingID)) {
            throw new System.ArgumentException("ID cannot be null or empty");
        }
        TrackingServer trackingServer = DependencyInjector.Ref().GetTrackingServer();
        _tracker = new Tracker(this, trackingServer);
    }

    public override void _PhysicsProcess(double delta) {
        if (_tracker == null) {
            throw new System.Exception("_tracker must be set before PhysicsProcess");
        }
        _tracker.Update();
    }

    public bool CheckPreconditions(Player player) {
        return Actions.All((InteractiveObjectAction action) => action.CheckPreconditions(this, player));
    }

    public bool Activate(Player player) {
        return Actions.All((InteractiveObjectAction action) => action.Activate(this, player));
    }

    public void AddAction(InteractiveObjectAction action) {
        Actions.Add(action);
    }
}

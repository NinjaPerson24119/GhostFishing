using Godot;
using System.Collections.Generic;

internal partial class InteractionUI : Control {
    private Timer refreshTimer = new Timer() {
        WaitTime = 0.1f,
        OneShot = true,
    };
    private InteractiveObject? selectedObject = null;
    private PlayerContext? _playerContext;

    public override void _Ready() {
        AddChild(refreshTimer);
        _playerContext = DependencyInjector.Ref().GetLocalPlayerContext(GetPath());
    }

    public override void _Process(double delta) {
        if (refreshTimer.IsStopped()) {
            refreshTimer.Start();
            Refresh();
        }

        Label label = GetNode<Label>("Label");
        if (selectedObject != null) {
            label.Text = selectedObject.Description;
        }
    }

    public override void _Input(InputEvent inputEvent) {
        if (_playerContext == null) {
            throw new System.Exception("PlayerContext must be set before _Input is called");
        }
        if (selectedObject == null) {
            return;
        }

        if (inputEvent.IsActionPressed("select")) {
            Player player = _playerContext.Player;
            bool result = selectedObject.Activate(player);
            if (!result) {
                GD.PrintErr($"Failed to activate {selectedObject.TrackingID}");
            }
        }
    }

    private void Refresh() {
        if (_playerContext == null) {
            throw new System.Exception("PlayerContext must be set before _Input is called");
        }

        Player player = _playerContext.Player;
        TrackingServer trackingServer = DependencyInjector.Ref().GetTrackingServer();
        Vector2I tile = trackingServer.GetTile(player.GlobalPosition);
        List<InteractiveObject> objects = trackingServer.GetObjectsInTileRadius<InteractiveObject>(tile, 1);


        List<InteractiveObject> filteredObjects = new List<InteractiveObject>();
        for (int i = 0; i < objects.Count; i++) {
            InteractiveObject obj = objects[i];
            if (!obj.CheckPreconditions(player)) {
                continue;
            }
            filteredObjects.Add(obj);
        }
        if (filteredObjects.Count == 0) {
            Visible = false;
            selectedObject = null;
            return;
        }

        float minDistanceSquared = float.MaxValue;
        for (int i = 0; i < filteredObjects.Count; i++) {
            InteractiveObject obj = filteredObjects[i];

            float distanceSquared = TrackerDistance.DistanceSquared(player.GlobalPosition, obj.TrackingPosition);
            obj.TrackingPosition.DistanceTo(player.GlobalPosition);
            if (distanceSquared < minDistanceSquared) {
                minDistanceSquared = distanceSquared;
                selectedObject = obj;
            }
        }

        Visible = true;
    }
}

using Godot;
using System.Collections.Generic;

internal partial class InteractionUI : Control {
    private Timer refreshTimer = new Timer() {
        WaitTime = 0.1f,
        OneShot = true,
    };
    private InteractiveObject? selectedObject = null;

    public override void _Ready() {
        AddChild(refreshTimer);
    }

    public override void _Process(double delta) {
        if (refreshTimer.IsStopped()) {
            refreshTimer.Start();
            Refresh();
        }

        Label label = GetNode<Label>("Label");
        if (selectedObject != null) {
            label.Text = selectedObject.Action.Description;
        }
    }

    public override void _Input(InputEvent inputEvent) {
        if (selectedObject == null) {
            return;
        }

        if (inputEvent.IsActionPressed("select")) {
            Player player = DependencyInjector.Ref().GetPlayer();
            bool result = selectedObject.Activate(player);
            if (!result) {
                GD.PrintErr($"Failed to activate {selectedObject.TrackingID}");
            }
        }
    }

    private void Refresh() {
        Player player = DependencyInjector.Ref().GetPlayer();
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

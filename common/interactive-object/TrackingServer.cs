using Godot;
using System.Collections.Generic;
using System.Linq;

internal partial class TrackingServer : Node {
    [Export(PropertyHint.Range, "1, 1000, 1")]
    public float TileSize {
        get => _tileSize;
        set {
            if (_tileSize != -1) {
                throw new System.InvalidOperationException("TileSize cannot be set more than once");
            }
            if (value <= 0) {
                throw new System.ArgumentOutOfRangeException("TileSize must be positive");
            }
            _tileSize = value;
        }
    }
    private float _tileSize = -1;

    // careful, this may hold stale references if objects are removed from the scene, but not from the server
    private Dictionary<Vector2I, List<ITrackableObject>> _tileToObjects = new Dictionary<Vector2I, List<ITrackableObject>>();
    private Dictionary<string, Vector2I> _objectToTile = new Dictionary<string, Vector2I>();

    public void Update(ITrackableObject obj) {
        if (obj == null) {
            throw new System.ArgumentNullException("obj cannot be null");
        }
        string ID = obj.TrackingID;
        GD.Print($"Updating {ID} to {obj.TrackingPosition}");

        // remove from old tile if exists
        Vector2I tileIndices = GetTile(obj.TrackingPosition);
        if (_objectToTile.ContainsKey(ID)) {
            Vector2I oldTileIndices = _objectToTile[ID];
            if (oldTileIndices == tileIndices) {
                return;
            }
            _tileToObjects[oldTileIndices].RemoveAll((ITrackableObject obj) => obj.TrackingID == ID);
        }
        _objectToTile[ID] = tileIndices;
        if (!_tileToObjects.ContainsKey(tileIndices)) {
            _tileToObjects[tileIndices] = new List<ITrackableObject>();
        }
        _tileToObjects[tileIndices].Add(obj);
    }

    public void Remove(ITrackableObject obj) {
        if (obj == null) {
            throw new System.ArgumentNullException("obj cannot be null");
        }
        string ID = obj.TrackingID;
        if (!_objectToTile.ContainsKey(ID)) {
            return;
        }
        Vector2I tileIndices = _objectToTile[ID];
        _tileToObjects[tileIndices].RemoveAll((ITrackableObject obj) => obj.TrackingID == ID);
        _objectToTile.Remove(ID);
        GD.Print($"(tracking server) Removed {obj.TrackingID}");
    }

    public Vector2I GetTile(Vector3 position) {
        return new Vector2I(
            Mathf.FloorToInt(position.X / _tileSize),
            Mathf.FloorToInt(position.Z / _tileSize)
        );
    }

    public List<T> GetObjectsInTileRadius<T>(Vector2I tileIndices, int radius = 0) where T : ITrackableObject {
        if (radius < 0) {
            throw new System.ArgumentOutOfRangeException("radius must be non-negative");
        }

        List<T> objects = new List<T>();
        for (int x = tileIndices.X - radius; x <= tileIndices.X + radius; x++) {
            for (int y = tileIndices.Y - radius; y <= tileIndices.Y + radius; y++) {
                Vector2I tile = new Vector2I(x, y);
                objects.AddRange(GetObjectsInTile<T>(tile));
            }
        }

        if (objects.Count != objects.Distinct().Count()) {
            throw new System.Exception("objects were not unique in tile radius");
        }
        return objects;
    }

    private List<T> GetObjectsInTile<T>(Vector2I tileIndices) where T : ITrackableObject {
        if (!_tileToObjects.ContainsKey(tileIndices)) {
            return new List<T>();
        }
        List<ITrackableObject> objects = _tileToObjects[tileIndices];
        List<T> objectsOfType = new List<T>();
        for (int i = 0; i < objects.Count; i++) {
            if (objects[i] is null) {
                throw new System.Exception("object in tile was null");
            }
            if (objects[i] is T) {
                objectsOfType.Add((T)objects[i]);
            }
        }
        return objectsOfType;
    }
}

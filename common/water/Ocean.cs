using System;
using Godot;

// Controls the ocean surface as a grid of water tiles
public partial class Ocean : Node3D {
    // The farthest distance from the origin that a water tile will be spawned
    [ExportGroup("Rendering")]
    [Export]
    public float ViewDistance {
        get {
            return _viewDistance;
        }
        set {
            _viewDistance = value;
            SetViewDistanceTiles();
            QueueRespawnWaterTiles();
        }
    }
    private float _viewDistance = 500f;
    private int _viewDistanceTiles;
    public void SetViewDistanceTiles() {
        _viewDistanceTiles = Mathf.CeilToInt(_viewDistance / TileSize);
    }

    // The distance to interpolate LOD tiles across
    // After this distance, the ocean will be rendered as a flat plane
    [Export]
    public float LODDistance {
        get {
            return _lodDistance;
        }
        set {
            _lodDistance = value;
            QueueRespawnWaterTiles();
        }
    }
    private float _lodDistance = 400f;

    // The minimum number of subdivisions for a water tile that isn't beyond the LOD distance
    [Export]
    public int MinSubdivisions {
        get {
            return _minSubdivisions;
        }
        set {
            _minSubdivisions = value;
            QueueRespawnWaterTiles();
        }
    }
    private int _minSubdivisions = 100;

    // Rounds the computed LOD subdivisions down to the nearest multiple of this value
    [Export]
    public int LODLevels {
        get {
            return _lodLevels;
        }
        set {
            _lodLevels = value;
            SetLODSubdivisionsSnap();
            QueueRespawnWaterTiles();
        }
    }
    private int _lodLevels = 20;
    private int _lodSubdivisionsSnap;
    public void SetLODSubdivisionsSnap() {
        _lodSubdivisionsSnap = Subdivisions / LODLevels;
    }

    [Export]
    public int Subdivisions {
        get {
            return _subdivisions;
        }
        set {
            _subdivisions = value;
            SetLODSubdivisionsSnap();
            QueueRespawnWaterTiles();
        }
    }
    private int _subdivisions = 400;

    [Export]
    public int TileSize {
        get {
            return _tileSize;
        }
        set {
            _tileSize = value;
            QueueRespawnWaterTiles();
        }
    }
    private int _tileSize = 100;

    [Export]
    public float TileOverlap {
        get {
            return _tileOverlap;
        }
        set {
            _tileOverlap = value;
            QueueRespawnWaterTiles();
        }
    }
    private float _tileOverlap = 0.001f;

    [ExportGroup("Waves")]
    [Export(PropertyHint.Range, "0,30,")]
    public int NoWaves {
        get {
            return _noWaves;
        }
        set {
            _noWaves = value;
            QueueReconfigureWaterTiles();
        }
    }
    private int _noWaves = 10;

    [Export]
    public float WaterDepth {
        get {
            return _waterDepth;
        }
        set {
            _waterDepth = value;
            QueueReconfigureWaterTiles();
        }
    }
    private float _waterDepth = 200f;

    [Export]
    public float WindAngle {
        get {
            return _windAngle;
        }
        set {
            _windAngle = value;
            QueueReconfigureWaterTiles();
        }
    }
    private float _windAngle = Mathf.Pi;

    [Export(PropertyHint.Range, "0.01,10,0.01")]
    public float Intensity {
        get {
            return _intensity;
        }
        set {
            _intensity = value;
            QueueReconfigureWaterTiles();
        }
    }
    private float _intensity = 0.5f;

    [Export(PropertyHint.Range, "0.0,1,0.01")]
    public float Damping {
        get {
            return _damping;
        }
        set {
            _damping = value;
            QueueReconfigureWaterTiles();
        }
    }
    private float _damping = 0.5f;

    [Export]
    public float SurfaceNoiseScale {
        get {
            return _surfaceNoiseScale;
        }
        set {
            _surfaceNoiseScale = value;
            QueueReconfigureWaterTiles();
        }
    }
    private float _surfaceNoiseScale = 10f;

    [Export]
    public float SurfaceHeightScale {
        get {
            return _surfaceHeightScale;
        }
        set {
            _surfaceHeightScale = value;
            QueueRespawnWaterTiles();
        }
    }
    private float _surfaceHeightScale = 0.5f;

    [Export]
    public float SurfaceTimeScale {
        get {
            return _surfaceTimeScale;
        }
        set {
            _surfaceTimeScale = value;
            QueueRespawnWaterTiles();
        }
    }
    private float _surfaceTimeScale = 0.02f;

    [ExportGroup("Debugging")]
    [Export]
    public bool WaterTileDebugLogs {
        get {
            return _waterTileDebugLogs;
        }
        set {
            _waterTileDebugLogs = value;
            QueueRespawnWaterTiles();
        }
    }
    private bool _waterTileDebugLogs = false;

    // the tile indices of the tile at the center of the ocean
    private Vector2 _originTileIndices = new Vector2(0, 0);

    public WaveSet? WavesConfig {
        get {
            return _wavesConfig;
        }
        set {
            if (_wavesConfig == null) {
                GD.Print("Ocean wave set to initial value");
            }
            else {
                GD.Print("Ocean wave set changed");
            }
            _wavesConfig = value;
            QueueReconfigureWaterTiles(false);
        }
    }
    private WaveSet? _wavesConfig;

    private bool _queuedRespawnWaterTiles = false;
    private bool _queuedReconfigureWaterTiles = false;
    private bool _queuedReconfigureWaterTilesDoRegenerateWaveSet = false;

    private const float _distantTileHeightOffset = -1f;

    public override void _Ready() {
        // unset queued flags to prevent duplicate events
        _queuedRespawnWaterTiles = false;
        _queuedReconfigureWaterTiles = false;
        _queuedReconfigureWaterTilesDoRegenerateWaveSet = false;

        SpawnWaterTiles();
    }

    private void FreeChildren() {
        foreach (Node child in GetChildren()) {
            RemoveChild(child);
            child.QueueFree();
        }
    }

    private void SpawnWaterTiles() {
        _queuedRespawnWaterTiles = false;

        SetViewDistanceTiles();
        SetLODSubdivisionsSnap();

        // make method idempotent
        FreeChildren();

        GD.Print($"Spawning ocean with view distance {ViewDistance} which is {_viewDistanceTiles} water tiles. ({Mathf.Pow(_viewDistanceTiles * 2 + 1, 2)} total tiles)");
        GenerateWaveSet();
        LinearLOD subdivisionsLOD = new LinearLOD(_lodDistance, Subdivisions, 0, _lodSubdivisionsSnap);
        for (int x = -_viewDistanceTiles; x <= _viewDistanceTiles; x++) {
            for (int z = -_viewDistanceTiles; z <= _viewDistanceTiles; z++) {
                float distanceToNearestTileEdge = TileDistanceFromOrigin(new Vector2(x, z));

                // avoid case where subdivisions aren't quite 0, but low enough to severely distort wave patterns
                int lodSubdivisions = subdivisionsLOD.ComputeLOD(distanceToNearestTileEdge);
                if (lodSubdivisions > 0) {
                    lodSubdivisions = Mathf.Max(lodSubdivisions, MinSubdivisions);
                }

                WaterTile waterTile = BuildWaterTile(new Vector2(x, z), lodSubdivisions);
                // if the tile is distant, move it down and disable waves
                if (Subdivisions > 0 && waterTile.Subdivisions == 0) {
                    waterTile.Position = new Vector3(waterTile.Position.X, waterTile.Position.Y + _distantTileHeightOffset, waterTile.Position.Z);
                    waterTile.NoDisplacement = true;
                }
                CallDeferred("add_child", waterTile);
            }
        }
        GD.Print("All water tiles spawned");
        GD.Print($"Reused {subdivisionsLOD.ReusedDistances} LOD distances");
    }

    private float TileDistanceFromOrigin(Vector2 tileIndices) {
        Vector2 position = tileIndices.Abs() * TileSize;
        return Mathf.Sqrt(Mathf.Pow(position.X, 2) + Mathf.Pow(position.Y, 2));
    }

    private WaterTile BuildWaterTile(Vector2 tileIndices, int subdivisions) {
        WaterTile waterTile = new WaterTile {
            Name = GetTileName(tileIndices),
            Position = new Vector3(tileIndices.X * TileSize, GlobalPosition.Y, tileIndices.Y * TileSize),
            // overlap slightly to prevent seams
            Scale = new Vector3(TileSize + TileOverlap, 1, TileSize + TileOverlap),
            Subdivisions = subdivisions,
            WavesConfig = WavesConfig,
            WaterTileDebugLogs = WaterTileDebugLogs,
            WaterDepth = WaterDepth,
            SurfaceNoiseScale = SurfaceNoiseScale,
            SurfaceHeightScale = SurfaceHeightScale,
            SurfaceTimeScale = SurfaceTimeScale,
            NoDisplacement = false,
        };
        return waterTile;
    }

    public override void _Process(double delta) {
        if (_queuedRespawnWaterTiles) {
            SpawnWaterTiles();
        }

        if (_queuedReconfigureWaterTiles) {
            ReconfigureWaterTiles();
        }
    }

    private string GetTileName(Vector2 indices) {
        return $"WaterTile_{indices.X},{indices.Y}";
    }

    private Vector2 AlignPositionToOceanOriginCorner(Vector2 globalXZ) {
        // note that the ocean is centered on the origin tile
        // e.g. the bounds for being in the origin tile are (-tileSize/2, tileSize/2)
        Vector2 alignedPosition = globalXZ + new Vector2(TileSize / 2, TileSize / 2);
        return alignedPosition;
    }

    // returns the tile indices relative to the ocean origin
    private Vector2 GetTileIndices(Vector2 globalXZ) {
        GD.Print($"Getting tile indices for {globalXZ}");
        Vector2 relativeToOcean = new Vector2(globalXZ.X - GlobalPosition.X, globalXZ.Y - GlobalPosition.Z);
        GD.Print($"Relative to ocean: {relativeToOcean}");
        Vector2 shiftedPosition = AlignPositionToOceanOriginCorner(relativeToOcean);
        GD.Print($"Shifted position: {shiftedPosition}");
        var t = new Vector2(Mathf.Floor(shiftedPosition.X / TileSize), Mathf.Floor(shiftedPosition.Y / TileSize));
        GD.Print($"Got tile indices: {t}");
        return t;
    }

    public Vector3 GetDisplacement(Vector2 globalXZ) {
        // delegate to water tile which may vary in configuration
        Vector2 tileIndices = GetTileIndices(globalXZ);
        string tileName = GetTileName(tileIndices);
        try {
            WaterTile waterTile = GetNode<WaterTile>(tileName);
            return waterTile.GetDisplacement(globalXZ);
        }
        catch (Exception) {
            GD.PrintErr($"Failed to GetDisplacement(). Couldn't find water tile {tileName}");
            return Vector3.Zero;
        }
    }

    // updates the central tile index when the origin changes to a new tile
    public void OnOriginChanged(Vector3 origin) {
        // returns the tile indices relative to the world origin
        Vector2 GlobalTileIndices(Vector3 position) {
            Vector2 shiftedPosition = AlignPositionToOceanOriginCorner(new Vector2(position.X, position.Z));
            return new Vector2(Mathf.Floor(shiftedPosition.X / TileSize), Mathf.Floor(shiftedPosition.Y / TileSize));
        }
        // determine which global tile the origin is in
        Vector2 newOriginTileIndices = GlobalTileIndices(origin);
        if (newOriginTileIndices != _originTileIndices) {
            // update the origin tile
            _originTileIndices = newOriginTileIndices;

            // recenter the ocean on the origin tile
            Position = new Vector3(_originTileIndices.X * TileSize, Position.Y, _originTileIndices.Y * TileSize);

            // verify that the origin tile is now (0,0)
            Vector2 indicesRelativeToOrigin = GetTileIndices(new Vector2(origin.X, origin.Z));
            DebugTools.Assert(indicesRelativeToOrigin == new Vector2(0, 0), $"Ocean origin tile is not (0,0) after recentering. Got {indicesRelativeToOrigin}");
        }
    }

    private void GenerateWaveSet() {
        WaveSetConfig config = WaveSet.BuildConfig(NoWaves, WindAngle, WaterDepth, Intensity, Damping);
        WavesConfig = new WaveSet(config);
    }

    public void ConfigureTileDebugVisuals(bool setting) {
        foreach (WaterTile waterTile in GetChildren()) {
            waterTile.SetDebugVisuals(setting);
        }
    }

    public void QueueRespawnWaterTiles() {
        GD.Print("Queuing respawn ocean water tiles");
        _queuedRespawnWaterTiles = true;
    }

    private void QueueReconfigureWaterTiles(bool regenerateWaveSet = true) {
        GD.Print("Queuing reconfigure ocean water tiles");
        _queuedReconfigureWaterTiles = true;
        _queuedReconfigureWaterTilesDoRegenerateWaveSet = regenerateWaveSet;
    }

    private void ReconfigureWaterTiles() {
        GD.Print("Reconfiguring water tiles");

        _queuedReconfigureWaterTiles = false;

        if (_queuedReconfigureWaterTilesDoRegenerateWaveSet) {
            GD.Print("Regenerating wave set in water tile reconfigure");
            GenerateWaveSet();
        }
        _queuedReconfigureWaterTilesDoRegenerateWaveSet = false;

        foreach (WaterTile waterTile in GetChildren()) {
            waterTile.WaterDepth = WaterDepth;
            waterTile.WaterTileDebugLogs = WaterTileDebugLogs;
            waterTile.WavesConfig = WavesConfig;
            waterTile.QueueReconfigureShaders();
        }
    }
}

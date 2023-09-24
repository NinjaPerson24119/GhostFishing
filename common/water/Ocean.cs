using System;
using Godot;

// Controls the ocean surface as a grid of water tiles
public partial class Ocean : Node3D {
	// The farthest distance from the origin that a water tile will be spawned
	[ExportGroup("Render Configuration")]
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
	private float _viewDistance = 1000f;
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

	// Rounds the computed LOD subdivisions down to the nearest multiple of this value
	[Export]
	public int LODSubdivisionsStep {
		get {
			return _lodSubdivisionsStep;
		}
		set {
			_lodSubdivisionsStep = value;
			QueueRespawnWaterTiles();
		}
	}
	private int _lodSubdivisionsStep = 10;

	[Export]
	public int Subdivisions {
		get {
			return _subdivisions;
		}
		set {
			_subdivisions = value;
			QueueRespawnWaterTiles();
		}
	}
	private int _subdivisions = 100;

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
	private int _tileSize = 20;

	[ExportGroup("Wave Configuration")]
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

	[ExportGroup("Debugging")]
	[Export]
	public bool WaterTileDebugLogs {
		get {
			return _waterTileDebugLogs;
		}
		set {
			_waterTileDebugLogs = value;
			QueueReconfigureWaterTiles();
		}
	}
	private bool _waterTileDebugLogs = false;

	// the tile indices of the tile at the center of the ocean
	private Vector2 _originTileIndices = new Vector2(0, 0);
	private WaveSet _waveSet;
	private bool _queuedRespawnWaterTiles = false;
	private bool _queuedReconfigureWaterTiles = false;
	private LinearLOD subdivisionsLOD;

	public override void _Ready() {
		SetViewDistanceTiles();
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

		// make method idempotent
		FreeChildren();

		GD.Print($"Spawning ocean with view distance {ViewDistance} which is {_viewDistanceTiles} water tiles. ({Mathf.Pow(_viewDistanceTiles * 2 + 1, 2)} total tiles)");
		GenerateWaveSet();
		subdivisionsLOD = new LinearLOD(_lodDistance, _subdivisions, 0, _lodSubdivisionsStep);
		for (int x = -_viewDistanceTiles; x <= _viewDistanceTiles; x++) {
			for (int z = -_viewDistanceTiles; z <= _viewDistanceTiles; z++) {
				float distanceToNearestTileEdge = TileDistance(new Vector2(x, z));
				int lodSubdivisions = subdivisionsLOD.ComputeLOD(distanceToNearestTileEdge);

				WaterTile waterTile = BuildWaterTile(new Vector2(x, z), lodSubdivisions);
				AddChild(waterTile);
			}
		}
		GD.Print($"Reused {subdivisionsLOD.ReusedDistances} LOD distances");
	}

	private float TileDistance(Vector2 tileIndices) {
		float x = Mathf.Abs(tileIndices.X);
		float z = Mathf.Abs(tileIndices.Y);
		return Mathf.Sqrt(Mathf.Pow(x, 2) + Mathf.Pow(z, 2)) * TileSize;
	}

	private WaterTile BuildWaterTile(Vector2 tileIndices, int subdivisions) {
		const float overlap = 0.1f;
		WaterTile waterTile = new WaterTile() {
			Name = GetTileName(tileIndices),
			Position = new Vector3(tileIndices.X * TileSize, GlobalPosition.Y, tileIndices.Y * TileSize),
			// overlap slightly to prevent seams
			Scale = new Vector3(TileSize + overlap, 1, TileSize + overlap),
			Subdivisions = subdivisions,
			WavesConfig = _waveSet,
			WaterTileDebugLogs = WaterTileDebugLogs,
			WaterDepth = WaterDepth,
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

	// returns the tile indices relative to the ocean origin
	private Vector2 GetTileIndices(Vector3 worldPosition) {
		// note that the ocean is centered on the origin tile
		// e.g. the bounds for being in the origin tile are (-tileSize/2, tileSize/2)
		Vector3 relativeToOcean = worldPosition - GlobalPosition;
		Vector3 shiftedRelativeToOcean = relativeToOcean + new Vector3(TileSize / 2, 0, TileSize / 2);
		return new Vector2(Mathf.Floor(shiftedRelativeToOcean.X / TileSize), Mathf.Floor(shiftedRelativeToOcean.Z / TileSize));
	}

	public float GetHeight(Vector3 worldPosition) {
		// delegate to water tile which may vary in configuration
		Vector2 tileIndices = GetTileIndices(worldPosition);
		string tileName = GetTileName(tileIndices);
		try {
			WaterTile waterTile = GetNode<WaterTile>(tileName);
			return waterTile.GetHeight(worldPosition);
		}
		catch (Exception) {
			GD.PrintErr($"Failed to GetHeight(). Couldn't find water tile {tileName}");
			return 0;
		}
	}

	// updates the central tile index when the origin changes to a new tile
	public void OnOriginChanged(Vector3 origin) {
		// returns the tile indices relative to the world origin
		Vector2 GlobalTileIndices(Vector3 position) {
			Vector3 shiftedPosition = position + new Vector3(TileSize / 2, 0, TileSize / 2);
			return new Vector2(Mathf.Floor(shiftedPosition.X / TileSize), Mathf.Floor(shiftedPosition.Z / TileSize));
		}
		// determine which global tile the origin is in
		Vector2 newOriginTileIndices = GlobalTileIndices(origin);
		if (newOriginTileIndices != _originTileIndices) {
			// update the origin tile
			_originTileIndices = newOriginTileIndices;

			// recenter the ocean on the origin tile
			Position = new Vector3(_originTileIndices.X * TileSize, Position.Y, _originTileIndices.Y * TileSize);

			// verify that the origin tile is now (0,0)
			Vector2 indicesRelativeToOrigin = GetTileIndices(origin);
			DebugTools.Assert(indicesRelativeToOrigin == new Vector2(0, 0), $"Ocean origin tile is not (0,0) after recentering. Got {indicesRelativeToOrigin}");
		}
	}

	private void GenerateWaveSet() {
		WaveSetConfig config = WaveSet.BuildConfig(NoWaves, WindAngle, WaterDepth, Intensity, Damping);
		_waveSet = new WaveSet(config);
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

	private void QueueReconfigureWaterTiles() {
		GD.Print("Queuing reconfigure ocean water tiles");
		_queuedReconfigureWaterTiles = true;
	}

	private void ReconfigureWaterTiles() {
		_queuedReconfigureWaterTiles = false;

		GD.Print("Reconfiguring water tiles");
		GenerateWaveSet();
		foreach (WaterTile waterTile in GetChildren()) {
			waterTile.WaterDepth = WaterDepth;
			waterTile.WaterTileDebugLogs = WaterTileDebugLogs;
			waterTile.WavesConfig = _waveSet;
			waterTile.QueueReconfigureShaders();
		}
	}
}

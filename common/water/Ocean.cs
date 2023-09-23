using System;
using Godot;

// Controls the ocean surface as a grid of water tiles
public partial class Ocean : Node3D {
	// the number of Ocean tiles in each direction from the origin
	[Export]
	public int ViewDistanceInTiles {
		get {
			return _viewDistanceInTiles;
		}
		set {
			_viewDistanceInTiles = value;
			SpawnWaterTiles();
		}
	}
	private int _viewDistanceInTiles = 3;

	[Export]
	public int NoWaves {
		get {
			return _noWaves;
		}
		set {
			_noWaves = value;
			OnWaveSetConfigurationChanged();
		}
	}
	private int _noWaves = 10;

	[Export]
	public int Subdivisions {
		get {
			return _subdivisions;
		}
		set {
			_subdivisions = value;
			SpawnWaterTiles();
		}
	}
	private int _subdivisions = 200;

	[Export]
	public float TileSize {
		get {
			return _tileSize;
		}
		set {
			_tileSize = value;
			SpawnWaterTiles();
		}
	}
	private float _tileSize = 50;

	[Export]
	public float WaterDepth {
		get {
			return _waterDepth;
		}
		set {
			_waterDepth = value;
			OnWaveSetConfigurationChanged();
		}
	}
	private float _waterDepth = 1000f;

	[Export]
	public float WindAngle {
		get {
			return _windAngle;
		}
		set {
			_windAngle = value;
			OnWaveSetConfigurationChanged();
		}
	}
	private float _windAngle = Mathf.Pi;

	[Export]
	public bool LogWaveParameters = false;

	[Signal]
	public delegate void RebuildShadersEventHandler();

	public double WaveTime = 0.0f;
	private ShaderMaterial Material;
	// the tile indices of the tile at the center of the ocean
	private Vector2 _originTileIndices = new Vector2(0, 0);
	private WaveSet _waveSet;

	public override void _Ready() {
		Material = GD.Load<ShaderMaterial>("res://common/water/Water.material");
		GenerateWaveSet();
		SpawnWaterTiles();

		GetNode<DebugMode>("/root/DebugMode").DebugOceanChanged += ConfigureTileDebugVisuals;
	}

	private void FreeChildren() {
		foreach (Node child in GetChildren()) {
			RemoveChild(child);
			child.QueueFree();
		}
	}

	private void SpawnWaterTiles() {
		// make method idempotent
		FreeChildren();

		for (int x = -ViewDistanceInTiles; x <= ViewDistanceInTiles; x++) {
			for (int z = -ViewDistanceInTiles; z <= ViewDistanceInTiles; z++) {
				WaterTile waterTile = BuildWaterTile(new Vector2(x, z));
				AddChild(waterTile);
			}
		}
	}

	private WaterTile BuildWaterTile(Vector2 tileIndices) {
		WaterTile waterTile = new WaterTile() {
			Name = GetTileName(tileIndices),
			Position = new Vector3(tileIndices.X * TileSize, GlobalPosition.Y, tileIndices.Y * TileSize),
			Scale = new Vector3(TileSize, 1, TileSize),
			Material = (ShaderMaterial)Material.Duplicate(),
			Mesh = new PlaneMesh() {
				Size = new Vector2(1, 1),
				SubdivideDepth = Subdivisions,
				SubdivideWidth = Subdivisions,
				Orientation = PlaneMesh.OrientationEnum.Y,
			},
			WavesConfig = _waveSet,
			LogWaveParameters = LogWaveParameters,
			WaterDepth = WaterDepth,
		};
		waterTile.Mesh.SurfaceSetMaterial(0, waterTile.Material);
		RebuildShaders += waterTile.OnRebuildShaders;
		return waterTile;
	}

	public override void _Process(double delta) {
		WaveTime += delta;
	}

	private string GetTileName(Vector2 indices) {
		return $"WaterTile_{indices.X},{indices.Y}";
	}

	private Vector2 GetTileIndices(Vector3 worldPosition) {
		Vector3 relativeToOcean = worldPosition - GlobalPosition;
		return new Vector2(Mathf.Floor(relativeToOcean.X / TileSize), Mathf.Floor(relativeToOcean.Z / TileSize));
	}

	public float GetHeight(Vector3 worldPosition) {
		// delegate to water tile which may vary in configuration
		Vector2 tileIndices = GetTileIndices(worldPosition);
		string tileName = GetTileName(tileIndices);
		try {
			WaterTile waterTile = GetNode<WaterTile>(tileName);
			return waterTile.GetHeight(worldPosition, WaveTime);
		}
		catch (Exception) {
			GD.PrintErr($"Failed to GetHeight(). Couldn't find water tile {tileName}");
			return 0;
		}
	}

	// updates the central tile index when the origin changes to a new tile
	public void OnOriginChanged(Vector3 origin) {
		// determine which tile the origin is in
		// it should always be in (0,0)
		Vector2 newOriginTileIndices = GetTileIndices(origin);
		if (newOriginTileIndices != _originTileIndices) {
			// update the origin tile
			_originTileIndices = newOriginTileIndices;

			// recenter the ocean on the origin tile
			Position = new Vector3(_originTileIndices.X * TileSize, Position.Y, _originTileIndices.Y * TileSize);
			DebugTools.Assert(GetTileIndices(origin) == new Vector2(0, 0), $"Origin tile is not (0,0) after recentering. Origin: {origin}, origin tile: {GetTileIndices(origin)}");
		}
	}

	private void GenerateWaveSet() {
		WaveSetConfig waveSetConfig = new WaveSetConfig() {
			noWaves = NoWaves,
			wavelengthAverage = 8f,
			wavelengthStdDev = 1f,
			amplitudeAverage = 0.1f,
			windAngleAverage = WindAngle,
			windAngleStdDev = Mathf.DegToRad(30f),
			waterDepth = WaterDepth,
		};
		_waveSet = new WaveSet(waveSetConfig);
	}

	public void OnWaveSetConfigurationChanged() {
		GenerateWaveSet();
		EmitSignal(SignalName.RebuildShaders);
	}

	public void ConfigureTileDebugVisuals(bool setting) {
		foreach (WaterTile waterTile in GetChildren()) {
			waterTile.SetDebugVisuals(setting);
		}
	}
}

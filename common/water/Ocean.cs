using System;
using Godot;

// Controls the ocean surface as a grid of water tiles
public partial class Ocean : Node3D {
	// TODO: export scene variables
	public double WaveTime = 0.0f;
	private ShaderMaterial Material;

	[Export]
	public int NoWaves {
		get {
			return _noWaves;
		}
		set {
			_noWaves = value;
			EmitSignal(SignalName.RebuildShaders);
		}
	}
	private int _noWaves = 10;

	// TODO: LOD subdivisions
	[Export]
	public int Subdivisions {
		get {
			return _subdivisions;
		}
		set {
			_subdivisions = value;
			BuildWaterTiles();
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
			BuildWaterTiles();
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
			EmitSignal(SignalName.RebuildShaders);
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
			EmitSignal(SignalName.RebuildShaders);
		}
	}
	private float _windAngle = Mathf.Pi;

	[Signal]
	public delegate void RebuildShadersEventHandler();

	public override void _Ready() {
		Material = GD.Load<ShaderMaterial>("res://common/water/Water.material");
		BuildWaterTiles();
	}

	private void FreeChildren() {
		foreach (Node child in GetChildren()) {
			RemoveChild(child);
			child.QueueFree();
		}
	}

	private void BuildWaterTiles() {
		FreeChildren();
		Vector2 tileIndices = GetTileIndices(GlobalPosition);
		WaterTile waterTile = new WaterTile() {
			Name = GetTileName(tileIndices),
			Position = new Vector3(0, 0, 0),
			Scale = new Vector3(TileSize, 1, TileSize),
			Material = (ShaderMaterial)Material.Duplicate(),
			Mesh = new PlaneMesh() {
				Size = new Vector2(1, 1),
				SubdivideDepth = Subdivisions,
				SubdivideWidth = Subdivisions,
				Orientation = PlaneMesh.OrientationEnum.Y,
			},
		};
		waterTile.Mesh.SurfaceSetMaterial(0, waterTile.Material);
		RebuildShaders += waterTile.OnRebuildShaders;
		AddChild(waterTile);
	}

	public override void _Process(double delta) {
		WaveTime += delta;
	}

	private string GetTileName(Vector2 indices) {
		return $"WaterTile_{indices.X},{indices.Y}";
	}

	private Vector2 GetTileIndices(Vector3 worldPosition) {
		return new Vector2(Mathf.Floor(worldPosition.X / TileSize), Mathf.Floor(worldPosition.Z / TileSize));
	}

	public float GetHeight(Vector3 worldPosition) {
		// delegate to water tile which may vary in configuration
		Vector2 tileIndices = GetTileIndices(worldPosition);
		string tileName = GetTileName(tileIndices);
		try {
			WaterTile waterTile = GetNode<WaterTile>(tileName);
			return waterTile.GetHeight(worldPosition, WaveTime);
		} catch (Exception) {
			GD.PrintErr($"Failed to GetHeight(). Couldn't find water tile {tileName}");
			return 0;
		}
	}
}

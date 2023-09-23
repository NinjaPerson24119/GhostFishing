using System;
using Godot;

// Controls the ocean surface as a grid of water tiles
public partial class Ocean : Node3D {
	// TODO: export scene variables
	public double WaveTime = 0.0f;
	private ShaderMaterial Material;

	[Export] public int TileRadius {
		get {
			return _tileRadius;
		}
		set {
			_tileRadius = value;
			BuildWaterTiles();
		}
	}
	private int _tileRadius = 1;

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

	[Export]
	public bool DebugWaves = false;

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
		for (int x = -TileRadius; x <= TileRadius; x++) {
			for (int z = -TileRadius; z <= TileRadius; z++) {
				WaterTile waterTile = DefaultWaterTile();
				waterTile.Position = new Vector3(x * TileSize, GlobalPosition.Y, z * TileSize);
				Vector2 tileIndices = GetTileIndices(waterTile.Position);
				GD.Print($"Building water tile {tileIndices}");
				waterTile.Name = GetTileName(tileIndices);
				
				AddChild(waterTile);
			}
		}
	}

	private WaterTile DefaultWaterTile() {
		// must set Name and Position after using this factory
		WaterTile waterTile = new WaterTile() {
			Scale = new Vector3(TileSize, 1, TileSize),
			Material = (ShaderMaterial)Material.Duplicate(),
			Mesh = new PlaneMesh() {
				Size = new Vector2(1, 1),
				SubdivideDepth = Subdivisions,
				SubdivideWidth = Subdivisions,
				Orientation = PlaneMesh.OrientationEnum.Y,
			},
			DebugWaves = DebugWaves,
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
}

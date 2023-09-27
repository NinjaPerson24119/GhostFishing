using Godot;

public partial class WaterTileCPU : MeshInstance3D {
    private Ocean _ocean;
    private Mesh _planeMesh;
    private WaterTile _waterTile;

    public override void _Ready() {
        _ocean = GetNode<Ocean>("Ocean");
        _planeMesh = Mesh;
        AddChild(BuildWaterTile(Vector2.Zero, _ocean.Subdivisions));
    }

    private WaterTile BuildWaterTile(Vector2 tileIndices, int subdivisions) {
        int TileSize = _ocean.TileSize;
        WaterTile waterTile = new WaterTile {
            Name = "WaterTile_GPU_for_CPU_Simulation",
            Position = new Vector3(tileIndices.X * TileSize, GlobalPosition.Y, tileIndices.Y * TileSize),
            // overlap slightly to prevent seams
            Scale = new Vector3(TileSize, 1, TileSize),
            Subdivisions = subdivisions,
            WavesConfig = _ocean.WavesConfig,
            WaterTileDebugLogs = false,
            WaterDepth = _ocean.WaterDepth,
            SurfaceNoiseScale = 10f,
            SurfaceHeightScale = 0.2f,
            SurfaceTimeScale = 0.025f,
            NoDisplacement = false,
            Visible = false, // don't actually render this one
        };
        return waterTile;
    }

    public override void _Process() {
        Vector3[] faceVertices = _planeMesh.GetFaces();
        Mesh
        mesh.
        var vertices = mesh.GetSurface(0).GetArrayMesh().GetSurfaceArrays()[ArrayMesh.ArrayType.Vertex];
        var normals = mesh.GetSurface(0).GetArrayMesh().GetSurfaceArrays()[ArrayMesh.ArrayType.Normal];
        var newVertices = new Vector3[vertices.Length];
        var newNormals = new Vector3[normals.Length];
        for (int i = 0; i < vertices.Length; i++) {
            var vertex = (Vector3)vertices[i];
            var normal = (Vector3)normals[i];
            var wave = _waveSet.GetWave(vertex.x, vertex.z);
            newVertices[i] = new Vector3(vertex.x, wave, vertex.z);
            newNormals[i] = new Vector3(normal.x, 1, normal.z);
        }
        mesh.SurfaceRemove(0);
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, newVertices, newNormals, new Vector2[0], new Color[0], new Vector2[0], new int[0]);
    }
}

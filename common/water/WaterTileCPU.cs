using Godot;

public partial class WaterTileCPU : MeshInstance3D {
    private WaveSet _waveSet;
    private Mesh _planeMesh;
    public override void _Ready() {
        _waveSet = GetNode<Ocean>("Ocean").WavesConfig;
        _planeMesh = Mesh;
        _waterTile
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

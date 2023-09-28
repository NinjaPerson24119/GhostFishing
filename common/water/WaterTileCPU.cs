using Godot;

public partial class WaterTileCPU : MeshInstance3D {
    private Ocean _ocean;
    private PlaneMesh _planeMesh;
    private WaterTile _waterTile;

    public override void _Ready() {
        _ocean = GetNode<Ocean>("/root/Main/Ocean");
        _planeMesh = Mesh as PlaneMesh;
    }

    public override void _Process(double delta) {
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, _planeMesh.GetMeshArrays());
        var mdt = new MeshDataTool();
        mdt.CreateFromSurface(mesh, 0);
        for (var i = 0; i < mdt.GetVertexCount(); i++) {
            Vector3 vertex = mdt.GetVertex(i);

            // the vertices are basically UV on [-0.5, 0.5]
            // scale them across the desired tile size
            vertex = vertex * 50;

            var displacement = _ocean.GetDisplacement(new Vector2(vertex.X, vertex.Z));
            vertex += displacement;
            mdt.SetVertex(i, vertex);
        }
        mesh.ClearSurfaces();
        mdt.SetMaterial(_planeMesh.SurfaceGetMaterial(0));
        mdt.CommitToSurface(mesh);
        Mesh = mesh;
    }
}

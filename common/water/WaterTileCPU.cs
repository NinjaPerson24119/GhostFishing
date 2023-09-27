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
        /*
        Vector3[] faceVertices = _planeMesh.GetFaces();
        var st = new SurfaceTool();
        st.SetMaterial(_planeMesh.SurfaceGetMaterial(0));
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < faceVertices.Length; i++) {
            var displacement = _ocean.GetDisplacement(faceVertices[i]);
            GD.Print($"Displacement: {displacement}");
            Vector3 newVertex = faceVertices[i] + displacement;
            st.AddVertex(newVertex);
        }
        Mesh = st.Commit();
        */

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, _planeMesh.GetMeshArrays());
        var mdt = new MeshDataTool();
        mdt.CreateFromSurface(mesh, 0);
        for (var i = 0; i < mdt.GetVertexCount(); i++) {
            Vector3 vertex = mdt.GetVertex(i);
            vertex += new Vector3(0, -0.01f * i, 0);
            mdt.SetVertex(i, vertex);
        }
        mesh.ClearSurfaces();
        mdt.SetMaterial(_planeMesh.SurfaceGetMaterial(0));
        mdt.CommitToSurface(mesh);
        Mesh = mesh;
    }
}

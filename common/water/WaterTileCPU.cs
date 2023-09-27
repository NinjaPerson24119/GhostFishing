using Godot;

public partial class WaterTileCPU : MeshInstance3D {
    private Ocean _ocean;
    private Mesh _planeMesh;
    private WaterTile _waterTile;

    public override void _Ready() {
        _ocean = GetNode<Ocean>("/root/Main/Ocean");
        _planeMesh = Mesh;
    }

    public override void _Process(double delta) {
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
    }
}

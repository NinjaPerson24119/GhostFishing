using Godot;

public partial class PlayerOrigin : MeshInstance3D
{
	public void OnReposition(Vector3 position) {
		Position = position;
	}
}

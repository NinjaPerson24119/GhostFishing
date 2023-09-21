using Godot;

public partial class Ocean : MeshInstance3D {
	ShaderMaterial material;
	Image noise;
	double noise_scale = 10.0;
	double height_scale = 0.15;
	double time_scale = 0.025;
	double wave_time = 0.0;

	public override void _Ready() {
		material = (ShaderMaterial)Mesh.SurfaceGetMaterial(0);

		var noiseTexture = (Noise)material.GetShaderParameter("wave");
		noise = noiseTexture.GetSeamlessImage(512, 512);

		material.SetShaderParameter("noise_scale", noise_scale);
		material.SetShaderParameter("height_scale", height_scale);
		material.SetShaderParameter("time_scale", time_scale);
	}

	public override void _Process(double delta) {
		wave_time += delta;
		material.SetShaderParameter("wave_time", wave_time);
	}

	private double GetWaveHeight(Vector3 worldPosition) {
		// should match the vertex shader algorithm
		int uv_x = (int)Mathf.Wrap(GlobalPosition.X / noise_scale + wave_time * time_scale, 0.0, 1.0);
		int uv_y = (int)Mathf.Wrap(GlobalPosition.Y / noise_scale + wave_time * time_scale, 0.0, 1.0);
		return noise.GetPixel(uv_x * noise.GetWidth(), uv_y * noise.GetHeight()).R * height_scale;
	}
}

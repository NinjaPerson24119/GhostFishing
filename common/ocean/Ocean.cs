using Godot;

public partial class Ocean : MeshInstance3D {
	ShaderMaterial material;
	Image noise;
	float noise_scale = 10.0f;
	float height_scale = 0.15f;
	float time_scale = 0.025f;
	double wave_time = 0.0f;
	float gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
	float water_depth = 10f;

	public override void _Ready() {
		material = (ShaderMaterial)Mesh.SurfaceGetMaterial(0);

		var noiseTexture = (NoiseTexture2D)material.GetShaderParameter("wave");
		noise = noiseTexture.Noise.GetSeamlessImage(512, 512);

		material.SetShaderParameter("noise_scale", noise_scale);
		material.SetShaderParameter("height_scale", height_scale);
		material.SetShaderParameter("time_scale", time_scale);

		float[] gerstner_amplitude = new float[] { 0.5f, 0.25f, 0.5f };
		float[] gerstner_phi = new float[] { 0, 0, 0 };
		float[] gerstner_k_x = new float[] { Mathf.Pi * 0.5f, 0.7f, 0.4f };
		float[] gerstner_k_z = new float[] { Mathf.Pi * 0.5f, 0.2f, 0.1f };
		float[] gerstner_k = new float[3];
		for (int i = 0; i < 3; i++) {
			gerstner_k[i] = Mathf.Sqrt(gerstner_k_x[i] * gerstner_k_x[i] + gerstner_k_z[i] * gerstner_k_z[i]);
		}
		float[] gerstner_omega = new float[3];
		for (int i = 0; i < 3; i++) {
			gerstner_omega[i] = Mathf.Sqrt(gravity * gerstner_k[i] * Mathf.Tanh(gerstner_k[i] * water_depth));
		}
		float[] gerstner_product_operand_x = new float[3];
		for (int i = 0; i < 3; i++) {
			gerstner_product_operand_x[i] = (gerstner_k_x[i] / gerstner_k[i]) * (gerstner_amplitude[i] / Mathf.Tanh(gerstner_k[i] * water_depth));
		}
		float[] gerstner_product_operand_z = new float[3];
		for (int i = 0; i < 3; i++) {
			gerstner_product_operand_z[i] = (gerstner_k_z[i] / gerstner_k[i]) * (gerstner_amplitude[i] / Mathf.Tanh(gerstner_k[i] * water_depth));
		}

		material.SetShaderParameter("gerstner_k_x", gerstner_k_x);
		material.SetShaderParameter("gerstner_k_z", gerstner_k_z);
		material.SetShaderParameter("gerstner_a", gerstner_amplitude);
		material.SetShaderParameter("gerstner_omega", gerstner_omega);
		material.SetShaderParameter("gerstner_phi", gerstner_phi);
		material.SetShaderParameter("gerstner_product_operand_x", gerstner_product_operand_x);
		material.SetShaderParameter("gerstner_product_operand_z", gerstner_product_operand_z);
	}

	public override void _Process(double delta) {
		wave_time += delta;
		material.SetShaderParameter("wave_time", wave_time);
	}

	public float GetHeight(Vector3 worldPosition) {
		// TODO: Archimedes principle
		// should match the vertex shader algorithm
		int uv_x = (int)Mathf.Wrap(worldPosition.X / noise_scale + wave_time * time_scale, 0.0, 1.0);
		int uv_y = (int)Mathf.Wrap(worldPosition.Z / noise_scale + wave_time * time_scale, 0.0, 1.0);
		return GlobalPosition.Y + noise.GetPixel(uv_x * noise.GetWidth(), uv_y * noise.GetHeight()).R * height_scale;
	}
}

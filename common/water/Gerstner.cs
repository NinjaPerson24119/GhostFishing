using Godot;

// CPU equivalent of the Gerstner shader
internal struct Gerstner {
    private WaveSet _waveSet;

    public Gerstner(WaveSet waveSet) {
        _waveSet = waveSet;
    }

    private float ThetaI(int i, float x, float z, float t) {
        Wave w = _waveSet.waves[i];
        return w.kX * x + w.kZ * z - w.angularFrequency * t - w.phaseShift;
    }

    // returned vector is in (X,Y,Z), the shader expects (X,Z,Y)
    public Vector3 Displacement(float x, float z, float t) {
        Vector3 result = Vector3.Zero;
        for (int i = 0; i < _waveSet.waves.Count; i++) {
            float theta = ThetaI(i, x, z, t);
            Wave w = _waveSet.waves[i];

            result.X -= w.productOperandX * Mathf.Sin(theta);
            result.Z -= w.productOperandZ * Mathf.Sin(theta);
            result.Y += w.amplitude * Mathf.Cos(theta);
        }
        return result;
    }

    public Vector3 Normal(float x, float z, float t) {
        float dxnew_dx = 1.0f;
        float dznew_dx = 0.0f;
        float dynew_dx = 0.0f;

        float dxnew_dz = 0.0f;
        float dznew_dz = 1.0f;
        float dynew_dz = 0.0f;

        for (int i = 0; i < _waveSet.waves.Count; i++) {
            float theta = ThetaI(i, x, z, t);
            Wave w = _waveSet.waves[i];

            float dtheta_dx = w.kX;
            float dtheta_dz = w.kZ;

            dxnew_dx -= w.productOperandX * Mathf.Cos(theta) * dtheta_dx;
            dznew_dx -= w.productOperandZ * Mathf.Cos(theta) * dtheta_dx;
            dynew_dx -= w.amplitude * Mathf.Sin(theta) * dtheta_dx;

            dxnew_dz -= w.productOperandX * Mathf.Cos(theta) * dtheta_dz;
            dznew_dz -= w.productOperandZ * Mathf.Cos(theta) * dtheta_dz;
            dynew_dz -= w.amplitude * Mathf.Sin(theta) * dtheta_dz;
        }

        Vector3 diff_vec_x = new Vector3(dxnew_dx, dynew_dx, dznew_dx);
        Vector3 diff_vec_z = new Vector3(dxnew_dz, dynew_dz, dznew_dz);

        return diff_vec_z.Cross(diff_vec_x).Normalized();
    }
}

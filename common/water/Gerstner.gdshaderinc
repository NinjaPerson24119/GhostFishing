// defines functions computing points on the surface of a gerstner wave

// skip waves with k = 0.0
const int max_waves = 12;

uniform float gerstner_k[max_waves]; // wave magnitude
uniform float gerstner_k_x[max_waves]; // wave direction x
uniform float gerstner_k_z[max_waves]; // wave direction y
uniform float gerstner_a[max_waves]; // amplitude
uniform float gerstner_omega[max_waves]; // angular frequency (dependent variable)
uniform float gerstner_phi[max_waves]; // phase
uniform float gerstner_product_operand_x[max_waves];
uniform float gerstner_product_operand_z[max_waves];

// wave i, at position x, z, at time t
float theta_i(int i, float x, float z, float t) {
	return gerstner_k_x[i] * x + gerstner_k_z[i] * z - gerstner_omega[i] * t - gerstner_phi[i];
}

vec3 gerstner(float x, float z, float t) {
	// this computes displacement, so we initialize the result to 0 instead of the inital vertex position
	vec3 result = vec3(0.0);
	for (int i = 0; i < max_waves; i++) {
		if (gerstner_k[i] != 0.0) {
			float theta = theta_i(i, x, z, t);
			result[0] -= gerstner_product_operand_x[i] * sin(theta);
			result[1] -= gerstner_product_operand_z[i] * sin(theta);
			result[2] += gerstner_a[i] * cos(theta);
		}
	}
	return result;
}

vec3 gerstner_normal(float x, float z, float t) {
	float dxnew_dx = 1.0;
	float dznew_dx = 0.0;
	float dynew_dx = 0.0;

	float dxnew_dz = 0.0;
	float dznew_dz = 1.0;
	float dynew_dz = 0.0;

	for (int i = 0; i < max_waves; i++) {
		if (gerstner_k[i] != 0.0) {
			float theta = theta_i(i, x, z, t);

			float dtheta_dx = gerstner_k_x[i];
			float dtheta_dz = gerstner_k_z[i];

			dxnew_dx -= gerstner_product_operand_x[i] * cos(theta) * dtheta_dx;
			dznew_dx -= gerstner_product_operand_z[i] * cos(theta) * dtheta_dx;
			dynew_dx -= gerstner_a[i] * sin(theta) * dtheta_dx;

			dxnew_dz -= gerstner_product_operand_x[i] * cos(theta) * dtheta_dz;
			dznew_dz -= gerstner_product_operand_z[i] * cos(theta) * dtheta_dz;
			dynew_dz -= gerstner_a[i] * sin(theta) * dtheta_dz;
		}
	}

	vec3 diff_vec_x = vec3(dxnew_dx, dynew_dx, dznew_dx);
	vec3 diff_vec_z = vec3(dxnew_dz, dynew_dz, dznew_dz);

	return normalize(cross(diff_vec_z, diff_vec_x));
}

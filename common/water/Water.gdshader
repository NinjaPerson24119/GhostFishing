shader_type spatial;
render_mode world_vertex_coords;

// DEBUG: for overdraw render
//render_mode blend_add, unshaded, world_vertex_coords;

#include "Gerstner.gdshaderinc"

uniform sampler2D DEPTH_TEXTURE : hint_depth_texture, filter_linear_mipmap;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

uniform float wave_time = 0; 

uniform vec3 albedo : source_color;
uniform vec3 albedo2 : source_color;
uniform float metallic : hint_range(0.0, 1.0) = 0;
uniform float roughness : hint_range(0.0, 1.0) = 0.2;
uniform sampler2D texture_normal;
uniform sampler2D texture_normal2;
uniform sampler2D wave;
uniform vec2 wave_direction = vec2(2.0, 0.0);
uniform vec2 wave_direction2 = vec2(0.0, 1.0);
uniform float time_scale : hint_range(0.0, 0.2, 0.005) = 0.025;

uniform float noise_scale = 10.0;
uniform float height_scale = 0.15;

uniform vec4 color_deep : source_color;
uniform vec4 color_shallow : source_color;
uniform float beers_law = 2.0;
uniform float depth_offset = -0.2;

uniform float edge_scale = 1;
uniform vec3 edge_color : source_color;

uniform bool debug_visuals;

uniform bool no_displacement;

varying float height;
varying vec3 world_pos;

float fresnel(float amount, vec3 normal, vec3 view) {
	return pow((1.0 - clamp(dot(normalize(normal), normalize(view)), 0.0, 1.0)), amount);
}

float linear_depth(float depth, vec2 screen_uv, mat4 inv_projection_matrix) {
	// NDC assumes Vulkan
	vec3 ndc = vec3(screen_uv * 2.0 - 1.0, depth);
	vec4 view = inv_projection_matrix * vec4(ndc, 1.0);
	view.xyz /= view.w;
	float linear_depth = -view.z;
	return linear_depth;
}

void vertex() {
	if (!no_displacement) {
		VERTEX += gerstner(VERTEX.x, VERTEX.z, wave_time).xzy;
		NORMAL = gerstner_normal(VERTEX.x, VERTEX.z, wave_time);

		// apply slight noise to waves
		// use direction of normal so it looks natural and we don't have to recompute the normal
		// don't apply around the tile edges or they won't connect properly
		const float d = 0.05;
		height = texture(wave, VERTEX.xz / noise_scale + wave_time * time_scale).x;
		if (UV.x > d && UV.x < 1.0-d && UV.y > d && UV.y < 1.0-d) {
			VERTEX += NORMAL * height * height_scale;
		} else {
			// reduce the noise gradually or we'll get an obvious line
			float gradualNoiseReduction = d;
			gradualNoiseReduction = min(gradualNoiseReduction, 1.0 - UV.x);
			gradualNoiseReduction = min(gradualNoiseReduction, UV.x);
			gradualNoiseReduction = min(gradualNoiseReduction, 1.0 - UV.y);
			gradualNoiseReduction = min(gradualNoiseReduction, UV.y);
			gradualNoiseReduction = smoothstep(0.0, d, gradualNoiseReduction);
			VERTEX += NORMAL * height * height_scale * gradualNoiseReduction;
		}
	}
}

void fragment() {
	// compute linear depth difference between fragment and depth buffer
	float depth_buffer_linear = linear_depth(texture(DEPTH_TEXTURE, SCREEN_UV).x, SCREEN_UV, INV_PROJECTION_MATRIX);
	float depth_fragment_linear = linear_depth(FRAGCOORD.z, SCREEN_UV, INV_PROJECTION_MATRIX);
	float depth_difference = depth_buffer_linear - depth_fragment_linear;

	float depth_texture = texture(DEPTH_TEXTURE, SCREEN_UV).x * 2.0 - 1.0;
	float depth = PROJECTION_MATRIX[3][2] / (depth_texture + PROJECTION_MATRIX[2][2]);
	float depth_blend = exp((depth+VERTEX.z + depth_offset) * -beers_law);
	depth_blend = clamp(1.0 - depth_blend, 0.0, 1.0);
	float depth_blend_power = clamp(pow(depth_blend, 2.5), 0.0, 1.0);

	vec3 screen_color = textureLod(SCREEN_TEXTURE, SCREEN_UV, depth_blend_power * 2.5).rgb;
	vec3 depth_color = mix(color_shallow.rgb, color_deep.rgb, depth_blend_power);
	vec3 color = mix(screen_color * depth_color, depth_color * 0.25, depth_blend_power * 0.5);

	vec2 time = (wave_time * wave_direction) * time_scale;
	vec2 time2 = (wave_time * wave_direction2) * time_scale;

	vec3 normal_blend = mix(texture(texture_normal,world_pos.xz + time).rgb, texture(texture_normal2,world_pos.xz + time2).rgb, 0.5);

	float fresnel = fresnel(5.0, NORMAL, VIEW);
	vec3 surface_color = mix(albedo, albedo2, fresnel);

	vec3 depth_color_adj = mix(edge_color, color, step(edge_scale, depth_difference));

	ALBEDO = clamp(surface_color + depth_color_adj, vec3(0.0), vec3(1.0));
	METALLIC = metallic;
	ROUGHNESS = roughness;
	NORMAL_MAP = normal_blend;

	// set alpha so Godot knows to render the water as transparent
	ALPHA = 0.9;

	// Outline the edges of the water tile black
	if (debug_visuals && (UV.x < 0.01 || UV.x > 0.99 || UV.y < 0.01 || UV.y > 0.99)) {
		ALBEDO = vec3(0.0);
		ALPHA = 1.0;
	}

	// DEBUG: Godot ignores custom shaders in debug draw mode, so we have to do this manually
	// render normals
	//ALBEDO = NORMAL;

	// render overdraw
	// need this at top: render_mode blend_add, unshaded, world_vertex_coords;
	//ALBEDO = vec3(0.4, 0.8, 0.8);
	//ALPHA = 0.1;
}

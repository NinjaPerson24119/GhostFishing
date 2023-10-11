shader_type canvas_item;

uniform vec3 background_color : source_color;
uniform vec3 outline_color : source_color;
uniform vec4 fill_color : source_color;

void fragment() {
    vec4 texture_color = texture(TEXTURE, UV);
    if (texture_color.b > 0.9) {
        COLOR = vec4(background_color, 1.0);
    } else if (texture_color.g > 0.9) {
        COLOR = vec4(outline_color.rgb, 1.0);
    } else if (texture_color.r > 0.9) {
        COLOR = vec4(fill_color.rgb, 1.0);
    } else {
        // yellow is vibrant error
        COLOR = vec4(1.0, 1.0, 0.0, 0.0);
    }
}

shader_type canvas_item;

uniform vec3 background_color : source_color;
uniform vec3 color : source_color;

void fragment() {
    if (texture(TEXTURE, UV).a > 0.0) {
        COLOR = vec4(color.rgb, 1.0);
    } else {
        COLOR = vec4(background_color, 1.0);
    }
}

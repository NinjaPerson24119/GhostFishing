shader_type canvas_item;

uniform vec3 color : hint_color;

void fragment() {
    if (texture(TEXTURE, UV).a > 0.0) {
        COLOR = vec4(color.rgb, 1.0);
    } else {
        COLOR = vec4(0.0, 0.0, 0.0, 0.0);
    }
}

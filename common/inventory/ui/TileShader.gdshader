shader_type canvas_item;

void fragment() {
    if (texture(TEXTURE, UV).a > 0.0) {
        // brown
        COLOR = vec4(0.72, 0.44, 0.10, 1.0);
    } else {
        COLOR = vec4(0.0, 0.0, 0.0, 0.0);
    }
}

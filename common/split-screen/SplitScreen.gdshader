shader_type canvas_item;

uniform float divider_size;
uniform sampler2D player_one_texture;
uniform sampler2D player_two_texture;

void fragment() {
    // vertical split
    if (UV.X < 0.5) {
        COLOR = texture(player_one_texture, vec2(UV.x * 2.0, UV.y));
    } else {
        COLOR = texture(player_two_texture, vec2((UV.x - 0.5) * 2.0, UV.y));
    }
    // border at split
    if (UV.X > 0.5 - divider_size.x && UV.X < 0.5 + divider_size.x) {
        COLOR = vec4(0.0, 0.0, 0.0, 1.0);
    }
}

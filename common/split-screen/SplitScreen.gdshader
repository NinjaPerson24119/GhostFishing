shader_type canvas_item;

uniform float divider_size = 0.01;
uniform sampler2D player_one_texture : source_color;
uniform sampler2D player_two_texture : source_color;

void fragment() {
    // vertical split
    if (UV.x < 0.5) {
        COLOR = texture(player_one_texture, vec2(UV.x * 2.0, UV.y));
    } else {
        COLOR = texture(player_two_texture, vec2((UV.x - 0.5) * 2.0, UV.y));
    }
    // border at split
    if (UV.x > 0.5 - divider_size && UV.x < 0.5 + divider_size) {
        COLOR = vec4(0.0, 0.0, 0.0, 1.0);
    }
}

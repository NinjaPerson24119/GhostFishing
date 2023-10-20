shader_type canvas_item;

uniform vec3 background_color : source_color;
uniform vec3 divider_size : source_color;
uniform vec3 divider_color : source_color;
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
        COLOR = divider_color;
    }
}

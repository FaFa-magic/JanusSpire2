shader_type canvas_item;
render_mode unshaded;

uniform sampler2D heart_mask : filter_linear, repeat_disable;
// Bounds of the existing visible Painting viewport in the background's UVs.
// The enlarged character/background registration itself stays unchanged.
uniform vec2 mask_origin_uv = vec2(0.0);
uniform vec2 mask_size_uv = vec2(1.0);

void fragment() {
    vec2 mask_uv = (UV - mask_origin_uv) / mask_size_uv;
    float inside = step(0.0, mask_uv.x) * step(mask_uv.x, 1.0)
        * step(0.0, mask_uv.y) * step(mask_uv.y, 1.0);
    // Native COLOR already contains the source texture, vertex color and fade.
    // Change alpha only; never tint/recompress the original illustration.
    COLOR.a *= texture(heart_mask, clamp(mask_uv, vec2(0.0), vec2(1.0))).a * inside;
}

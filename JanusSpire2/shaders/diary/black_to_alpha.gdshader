shader_type canvas_item;

uniform float black_cutoff : hint_range(0.0, 0.2) = 0.02;
uniform float edge_softness : hint_range(0.001, 0.2) = 0.045;

void fragment() {
	vec4 color = texture(TEXTURE, UV);
	float brightness = max(max(color.r, color.g), color.b);
	float alpha_mask = smoothstep(black_cutoff, black_cutoff + edge_softness, brightness);
	COLOR = vec4(color.rgb, color.a * alpha_mask);
}

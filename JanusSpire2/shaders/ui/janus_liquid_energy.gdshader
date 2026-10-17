shader_type canvas_item;
render_mode unshaded;

uniform float fill_ratio : hint_range(0.0, 1.0) = 1.0;
uniform float empty_opacity : hint_range(0.0, 1.0) = 0.38;
uniform float motion_time = 1.65;
uniform float motion_strength : hint_range(0.0, 1.0) = 0.0;
uniform vec2 orb_center = vec2(0.5, 0.5);
uniform float orb_radius = 0.4048;

// All glass colors and reflections are in the PNG. This material only animates
// the fill mask, surface highlight and bubbles, so the image is reusable alone.
varying vec4 canvas_tint;

void vertex() {
    canvas_tint = COLOR;
}

void fragment() {
    vec4 tint = canvas_tint;
    vec2 p = (UV - orb_center) / orb_radius;
    vec4 artwork = texture(TEXTURE, UV);
    float fill = clamp(fill_ratio, 0.0, 1.0);
    // Height, not circular area: 2/3 energy leaves the top 1/3 unfilled.
    float level = 1.0 - 2.0 * fill;
    float decay = exp(-3.6 * motion_time) * motion_strength;
    float endpoint = smoothstep(0.0, 0.06, fill) * (1.0 - smoothstep(0.94, 1.0, fill));
    float wave = (p.x * sin(motion_time * 11.0)
        + 0.32 * sin(p.x * 5.0 + motion_time * 13.0)) * 0.071 * decay * endpoint;
    float surface = level + wave;
    float edge = max(fwidth(p.y), 0.003);
    float wet = smoothstep(surface - edge, surface + edge, p.y);
    if (fill >= 0.9999) { wet = 1.0; }
    if (fill <= 0.0001) { wet = 0.0; }

    vec3 glass = artwork.rgb;
    float surface_glint = (1.0 - smoothstep(0.004, 0.018, abs(p.y - surface))) * endpoint;
    glass += vec3(0.17, 0.20, 0.22) * surface_glint;

    // Eight tiny bubbles, wholly clipped to the orb and the filled region.
    // They rise only after an energy change and disappear before the water settles.
    float bubbles = 0.0;
    for (int i = 0; i < 8; i++) {
        float seed = float(i);
        float age = motion_time - seed * 0.055;
        float duration = 0.65 + fract(seed * 0.37) * 0.40;
        float progress = clamp(age / duration, 0.0, 1.0);
        float x = (fract(seed * 0.618 + 0.17) * 2.0 - 1.0) * 0.76;
        x += sin(seed * 2.4 + age * 5.0) * 0.012;
        float y = level + (1.0 - progress) * (0.138 + fract(seed * 0.43) * 0.435);
        float size = 0.0095 + fract(seed * 0.29) * 0.0071;
        float distance_to_bubble = length(p - vec2(x, y));
        float ring = 1.0 - smoothstep(size * 0.30, size * 0.30 + 0.0063, abs(distance_to_bubble - size));
        bubbles += ring * sin(progress * PI) * step(0.0, age) * (1.0 - step(duration, age));
    }
    glass = mix(glass, vec3(0.90, 0.98, 1.0), clamp(bubbles * motion_strength * wet * 0.60, 0.0, 0.8));

    float opacity = mix(empty_opacity, 1.0, wet);
    COLOR = vec4(glass * tint.rgb, artwork.a * opacity * tint.a);
}

shader_type canvas_item;
render_mode unshaded;

// NTransition drives 0 -> 1 when leaving character selection.
// The endpoints must remain transparent -> opaque black for scene loading.
uniform sampler2D transitionTex : source_color, filter_linear, repeat_disable;
uniform float threshold : hint_range(0.0, 1.0) = 0.0;
uniform float book_height : hint_range(0.3, 0.9) = 0.72;

const float BOOK_ASPECT = 1.8;
const float SPINE = 0.50;
const float PAGE_WIDTH = 0.393;
const float PAGE_TOP = 0.076;
const float PAGE_BOTTOM = 0.844;

float hash_line(float x) {
	return fract(sin(x * 127.1 + 17.7) * 43758.5453);
}

float diary_ink(vec2 p, float entry) {
	// Quiet handwriting-like marks; no arbitrary generated readable text.
	float row = floor((p.y - 0.21) * 19.0);
	float baseline = 0.21 + (row + 0.5) / 19.0;
	baseline += 0.0023 * sin(p.x * 53.0 + row * 2.7);
	float stroke = 1.0 - smoothstep(0.0012, 0.0028, abs(p.y - baseline));
	float end = 0.62 + 0.25 * hash_line(row + entry * 13.0);
	float span = smoothstep(0.115, 0.125, p.x) * (1.0 - smoothstep(end, end + 0.01, p.x));
	float words = smoothstep(0.12, 0.22, fract(p.x * (8.0 + hash_line(row) * 6.0) + row));
	float paragraph = 1.0 - step(0.5, mod(row + 1.0, 5.0) / 8.0);
	return stroke * span * words * paragraph * step(0.21, p.y) * step(p.y, 0.78);
}

vec4 book_sample(vec2 p) {
	vec4 c = texture(transitionTex, clamp(p, vec2(0.0), vec2(1.0)));
	float brightness = max(c.r, max(c.g, c.b));
	c.a *= smoothstep(0.02, 0.065, brightness);
	c.a *= step(0.0, p.x) * step(p.x, 1.0) * step(0.0, p.y) * step(p.y, 1.0);
	return c;
}

vec3 paper_sample(vec2 p, bool backside, float entry) {
	float tx = backside ? mix(0.49, 0.107, p.x) : mix(0.508, 0.893, p.x);
	vec3 paper = texture(transitionTex, vec2(tx, mix(PAGE_TOP, PAGE_BOTTOM, p.y))).rgb;
	paper = mix(paper, vec3(0.24, 0.28, 0.34), diary_ink(p, entry) * 0.38);
	return paper;
}

void fragment() {
	float t = clamp(threshold, 0.0, 1.0);
	// SCREEN_UV keeps the book centered despite the official oversized ColorRect.
	float aspect = SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x;
	vec2 centered = (SCREEN_UV - vec2(0.5)) * vec2(aspect, 1.0);
	float height = min(book_height, aspect * 0.88 / BOOK_ASPECT);
	float settle = 0.965 + 0.035 * smoothstep(0.0, 0.18, t);
	vec2 uv = centered / vec2(height * BOOK_ASPECT, height) / settle + vec2(0.5);
	uv.y += 0.012 * (1.0 - smoothstep(0.0, 0.18, t));
	vec4 book = book_sample(uv);
	// At the normal 2 s duration: appear 0-.4 s, turn twice .44-1.48 s,
	// then fade the entire diary into black from 1.56-2 s.
	float turns = clamp((t - 0.22) / 0.52, 0.0, 1.0) * 2.0;
	float entry = floor(turns);
	float phase = fract(turns);
	float angle = PI * smoothstep(0.0, 1.0, phase);
	float turn_active = step(0.22, t) * (1.0 - step(0.74, t));
	float bend = sin(angle) * turn_active;
	float cosine = cos(angle);
	float safe_cos = max(abs(cosine), 0.008) * (cosine < 0.0 ? -1.0 : 1.0);
	vec2 resting_page = vec2(abs(uv.x - SPINE) / PAGE_WIDTH, (uv.y - PAGE_TOP) / (PAGE_BOTTOM - PAGE_TOP));
	float on_page = step(0.0, resting_page.x) * step(resting_page.x, 1.0) * step(0.0, resting_page.y) * step(resting_page.y, 1.0);
	book.rgb = mix(book.rgb, vec3(0.24, 0.28, 0.34), diary_ink(resting_page, entry + step(SPINE, uv.x)) * on_page * 0.38);

	// Moving shadow falls onto the stationary pages around the book spine.
	float shadow = exp(-abs(uv.x - SPINE) * 23.0) * bend * on_page * 0.36;
	book.rgb *= 1.0 - shadow;
	float u = (uv.x - SPINE) / (PAGE_WIDTH * safe_cos);
	float lift = bend * 0.082 * sin(clamp(u, 0.0, 1.0) * PI * 0.5);
	float page_top = PAGE_TOP - lift;
	float page_bottom = PAGE_BOTTOM - lift * 0.38;
	float v = (uv.y - page_top) / (page_bottom - page_top);
	float aa_x = max(fwidth(u), 0.001);
	float aa_y = max(fwidth(v), 0.001);
	float page_alpha = smoothstep(0.0, aa_x, u) * (1.0 - smoothstep(1.0 - aa_x, 1.0, u));
	page_alpha *= smoothstep(0.0, aa_y, v) * (1.0 - smoothstep(1.0 - aa_y, 1.0, v)) * turn_active;
	vec3 paper = paper_sample(clamp(vec2(u, v), vec2(0.0), vec2(1.0)), cosine < 0.0, entry + 1.0);
	float shading = 1.0 - bend * 0.22 + bend * 0.10 * sin(clamp(u, 0.0, 1.0) * PI);
	paper *= shading;
	book.rgb = mix(book.rgb, paper, page_alpha);
	book.a = max(book.a, page_alpha);

	float appear = smoothstep(0.0, 0.20, t);
	float disappear = 1.0 - smoothstep(0.78, 1.0, t);
	float book_alpha = book.a * appear * disappear;
	// NTransition already supplies a gradual quadratic black child overlay.
	// Our additional black fill starts only after both page turns are finished.
	float black_alpha = smoothstep(0.78, 1.0, t);
	float out_alpha = book_alpha + black_alpha * (1.0 - book_alpha);
	// Explicit RGB is necessary: NTransition is a black ColorRect.
	COLOR = vec4(book.rgb * book_alpha / max(out_alpha, 0.00001), out_alpha);
}

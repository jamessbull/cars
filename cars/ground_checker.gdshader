shader_type spatial;

uniform vec3 color_light : source_color = vec3(0.45, 0.75, 0.3);
uniform vec3 color_dark : source_color = vec3(0.25, 0.55, 0.15);
uniform float tile_count : hint_range(1.0, 100.0) = 10.0;

void fragment() {
	vec2 uv = UV * tile_count;
	float checker = mod(floor(uv.x) + floor(uv.y), 2.0);
	ALBEDO = mix(color_light, color_dark, checker);
}

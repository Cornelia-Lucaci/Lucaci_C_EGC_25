
#version 330 core

uniform vec2 uResolution;
uniform float uTime;

out vec4 FragColor;

// Generator de valori aleatoare 2D
float hash(vec2 p)
{
    return fract(
        sin(dot(p, vec2(127.1, 311.7))) * 43758.5453
    );
}

void main()
{
    vec2 uv = gl_FragCoord.xy /
        max(uResolution, vec2(1.0));

    // Fundal cosmic bleumarin
    vec3 top = vec3(0.025, 0.045, 0.095);
    vec3 bottom = vec3(0.003, 0.006, 0.018);

    vec3 color = mix(bottom, top, uv.y);

    // Stele mici distribuite pe ecran
    vec2 pixel = gl_FragCoord.xy;

    float cellSize = 8.0;
    vec2 cell = floor(pixel / cellSize);
    vec2 local = fract(pixel / cellSize);

    float randomX = hash(cell);
    float randomY = hash(cell + vec2(17.3, 9.2));

    vec2 starPosition = vec2(randomX, randomY) * 0.8 + 0.1;

    vec2 difference = (local - starPosition) * cellSize;
    float distanceToStar = length(difference);

    // Selectam celulele care contin stele
    float chance = hash(cell + vec2(41.7, 23.1));
    float visible = step(0.88, chance);

    // Dimensiunea stelelor
    float radius = 0.35 +
        hash(cell + vec2(5.2, 8.4)) * 0.65;

    float star = 1.0 - smoothstep(
        radius * 0.45,
        radius + 0.45,
        distanceToStar
    );

    // Clipire discreta
    float phase = hash(cell + vec2(12.4, 31.8)) * 6.28318;

    float twinkle = 0.88 +
        0.12 * sin(uTime * 1.5 + phase);

    float brightness = visible * star * twinkle;

    // Culori usor diferite ale stelelor
    vec3 starColor = mix(
        vec3(0.65, 0.78, 1.0),
        vec3(1.0, 1.0, 1.0),
        hash(cell + vec2(3.1, 7.8))
    );

    color += starColor * brightness * 1.2;

    // Stele mai mari si mai luminoase
    vec2 bigCell = floor(pixel / 65.0);
    vec2 bigLocal = fract(pixel / 65.0);

    float bigChance = hash(bigCell + vec2(91.2, 13.7));

    vec2 bigPosition = vec2(
        hash(bigCell + vec2(4.1, 2.3)),
        hash(bigCell + vec2(8.7, 6.2))
    );

    float bigDistance = length(
        (bigLocal - bigPosition) * 65.0
    );

    float bigStar = 1.0 - smoothstep(
        0.7,
        1.8,
        bigDistance
    );

    float bigVisible = step(0.96, bigChance);

    float bigTwinkle = 0.9 +
        0.1 * sin(uTime * 0.8 + bigChance * 20.0);

    color += vec3(0.85, 0.92, 1.0) *
        bigStar * bigVisible * bigTwinkle * 1.8;

    // Vigneta discreta
    float d = distance(uv, vec2(0.5));

    color *= 1.0 - 0.25 * smoothstep(0.4, 0.95, d);

    FragColor = vec4(color, 1.0);
}

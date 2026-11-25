
#version 330 core

in vec3 vColor;
in vec2 vUV;
in vec3 vLocalPos;

uniform float uTime;
uniform bool uShowUV;

uniform vec3 uSeed;
uniform float uLandScale;
uniform float uSeaLevel;

uniform float uCloudScale;
uniform float uCloudSpeed;
uniform float uIceLatitude;
uniform float uCloudOpacity;

out vec4 FragColor;

// Hash 3D
float hash(vec3 p)
{
    p = fract(p * 0.3183099 + vec3(0.1));
    p *= 17.0;

    return fract(
        p.x * p.y * p.z *
        (p.x + p.y + p.z)
    );
}

// Interpolare a zgomotului 3D folosind 8 colturi
float noise(vec3 p)
{
    vec3 i = floor(p);
    vec3 f = fract(p);

    f = f * f * (3.0 - 2.0 * f);

    float n000 = hash(i);
    float n100 = hash(i + vec3(1, 0, 0));
    float n010 = hash(i + vec3(0, 1, 0));
    float n110 = hash(i + vec3(1, 1, 0));

    float n001 = hash(i + vec3(0, 0, 1));
    float n101 = hash(i + vec3(1, 0, 1));
    float n011 = hash(i + vec3(0, 1, 1));
    float n111 = hash(i + vec3(1, 1, 1));

    float x00 = mix(n000, n100, f.x);
    float x10 = mix(n010, n110, f.x);

    float x01 = mix(n001, n101, f.x);
    float x11 = mix(n011, n111, f.x);

    float y0 = mix(x00, x10, f.y);
    float y1 = mix(x01, x11, f.y);

    return mix(y0, y1, f.z);
}

// FBM cu 5 octave
float fbm(vec3 p)
{
    float value = 0.0;
    float amplitude = 0.5;

    for (int i = 0; i < 5; i++)
    {
        value += amplitude * noise(p);

        p *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 0.0, 1.0);
        return;
    }

    // Pozitia normalizata a punctului de pe sfera
    vec3 dir = normalize(vLocalPos);

    // Zgomotul principal pentru continente
    float terrain = fbm(
        dir * uLandScale + uSeed
    );

    // Separarea oceanului de uscat
    float land = smoothstep(
        uSeaLevel - 0.012,
        uSeaLevel + 0.012,
        terrain
    );

    // Inaltimea normalizata a terenului
    float elevation = max(
        (terrain - uSeaLevel) / (1.0 - uSeaLevel),
        0.0
    );

    // Culorile planetei
    vec3 deepOcean = vec3(0.008, 0.045, 0.18);
    vec3 ocean = vec3(0.015, 0.16, 0.40);
    vec3 shallowWater = vec3(0.035, 0.36, 0.57);

    vec3 beach = vec3(0.76, 0.68, 0.45);
    vec3 grass = vec3(0.19, 0.48, 0.17);
    vec3 forest = vec3(0.045, 0.25, 0.10);

    vec3 mountain = vec3(0.38, 0.36, 0.32);
    vec3 highMountain = vec3(0.55, 0.54, 0.50);
    vec3 snow = vec3(0.91, 0.95, 1.0);

    // OCEANE
    float depth = smoothstep(
        0.20,
        uSeaLevel,
        terrain
    );

    vec3 color = mix(
        deepOcean,
        ocean,
        depth
    );

    // Apa mai deschisa langa tarm
    float coast = smoothstep(
        uSeaLevel - 0.045,
        uSeaLevel + 0.005,
        terrain
    );

    color = mix(
        color,
        shallowWater,
        coast * (1.0 - land)
    );

    // PLAJE
    float beaches = land * (
        1.0 - smoothstep(0.015, 0.055, elevation)
    );

    color = mix(
        color,
        beach,
        beaches
    );

    // CAMPII
    float plains = land * smoothstep(
        0.025,
        0.10,
        elevation
    );

    color = mix(
        color,
        grass,
        plains
    );

    // PADURI - zgomot separat pentru distributia vegetatiei
    float forestNoise = fbm(
        dir * 22.0 + uSeed + vec3(7.3, 2.1, 5.8)
    );

    float forests = plains *
        smoothstep(0.48, 0.66, forestNoise) *
        (1.0 - smoothstep(0.22, 0.38, elevation));

    color = mix(
        color,
        forest,
        forests
    );

    // MUNTI
    float mountains = land * smoothstep(
        0.28,
        0.43,
        elevation
    );

    color = mix(
        color,
        mountain,
        mountains
    );

    // Munti inalti
    float highlands = mountains * smoothstep(
        0.48,
        0.62,
        elevation
    );

    color = mix(
        color,
        highMountain,
        highlands
    );

    // ZAPADA PE VARFURILE MUNTILOR
    float mountainSnow = mountains * smoothstep(
        0.62,
        0.78,
        elevation
    );

    color = mix(
        color,
        snow,
        mountainSnow
    );

    // CALOTE POLARE CU MARGINE NEREGULATA
    float iceNoise = fbm(
        dir * 13.0 + uSeed * 0.35
    );

    float iceBoundary = uIceLatitude +
        (iceNoise - 0.5) * 0.09;

    float polar = smoothstep(
        iceBoundary - 0.015,
        iceBoundary + 0.015,
        abs(dir.y)
    );

    color = mix(
        color,
        snow,
        polar
    );

    // NORI - zgomot independent de samanta continentelor
    vec3 cloudPosition = dir * uCloudScale +
        vec3(uTime * uCloudSpeed, 0.0, 0.0);

    float cloudNoise = fbm(
        cloudPosition + vec3(4.2, 8.1, 2.7)
    );

    float clouds = smoothstep(
        0.57,
        0.70,
        cloudNoise
    );

    color = mix(
        color,
        vec3(0.96, 0.98, 1.0),
        clouds * uCloudOpacity * 0.72
    );

    // Iluminare pentru efect de volum
    vec3 lightDirection = normalize(
        vec3(-0.5, 0.4, 1.0)
    );

    float lighting = 0.70 +0.30 * max(dot(dir, lightDirection), 0.0);

    color *= lighting * 1.15;

    FragColor = vec4(color * vColor, 1.0);
}

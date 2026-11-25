#version 330 core

// P3: modele repetitive. Coordonatele UV se inmultesc cu uScale, deci pe panou
// incap uScale "celule"; fiecare functie intoarce 0 (culoarea A) sau 1 (culoarea B).

in vec3 vColor;
in vec2 vUV;

uniform bool uShowUV;
uniform vec3 uColorA;
uniform vec3 uColorB;
uniform int uPattern;       // 0 = dungi, 1 = tabla de sah, 2 = grila
uniform float uScale;       // cate celule pe latura panoului
uniform float uLineWidth;   // grosimea liniilor grilei, ca fractiune din celula

out vec4 FragColor;

// Dungi verticale: jumatate din fiecare celula A, jumatate B.
float stripes(vec2 p)
{
  return step(0.5, fract(p.x));
}

// Tabla de sah: celulele alterneaza pe ambele axe.
float checker(vec2 p)
{
  return mod(floor(p.x) + floor(p.y), 2.0);
}

// Grila: linii subtiri la marginea stanga si la marginea de jos a fiecarei celule.
float gridLines(vec2 p)
{
   vec2 f = fract(p);
   return 1.0 - step(uLineWidth, f.x) * step(uLineWidth, f.y);
}

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 0.0, 1.0);
        return;
    }

    vec2 p = vUV * uScale;
    float m;
    if (uPattern == 0)
        m = stripes(p);
    else if (uPattern == 1)
        m = checker(p);
    else
        m = gridLines(p);

    vec3 color = mix(uColorA, uColorB, m);
    FragColor = vec4(color * vColor, 1.0);
}

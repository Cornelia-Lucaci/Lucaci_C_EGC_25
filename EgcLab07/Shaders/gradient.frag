#version 330 core

// P2: degradeul intre doua culori, dupa o directie data de un unghi.

in vec3 vColor;
in vec2 vUV;

uniform bool uShowUV;     // tasta U: afiseaza coordonatele UV (rosu = u, verde = v)
uniform vec3 uColorA;     // culoarea de la inceputul degradeului
uniform vec3 uColorB;     // culoarea de la sfarsitul degradeului
uniform float uAngle;     // directia degradeului, in grade (0 = de la stanga la dreapta, 90 = de jos in sus)
uniform int uSteps;       // 0 = degrade continuu; n >= 2 = n trepte de culoare

out vec4 FragColor;

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 0.0, 1.0);
        return;
    }

      // 1. Stabilim directia degradeului
    vec2 dir = vec2(cos(radians(uAngle)), sin(radians(uAngle)));

    // 2. Calculam pozitia pe directia degradeului
    float t = clamp(dot(vUV - vec2(0.5), dir) + 0.5, 0.0, 1.0);

    // 3. Aplicam treptele de culoare, daca sunt solicitate
    if (uSteps >= 2)
    {
        float n = float(uSteps);
        t = min(floor(t * n), n - 1.0) / (n - 1.0);
    }

    // 4. Interpolam intre cele doua culori
    vec3 color = mix(uColorA, uColorB, t);

    FragColor = vec4(color * vColor, 1.0);
}

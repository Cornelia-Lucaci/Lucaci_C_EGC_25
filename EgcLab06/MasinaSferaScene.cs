
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public class MasinaSferaScene : Scene
{
    private SceneObject _sfera = null!;
    private float _unghi = 0.0f;
    private float _deplasareZ = 0.0f;

    // Retinem toate componentele masinii
    private readonly List<(SceneObject Obiect, float ZInitial)> _masina = new();

    public override string Name => "Masina si sfera";

    public override void Load(Input input)
    {
        // Camera laterala
        CameraPosition = new Vector3(10.0f, 3.5f, 0.0f);
        CameraTarget = new Vector3(0.0f, 0.8f, 0.0f);

        // Controale
        input.Bind("stanga", Keys.A, Keys.Left);
        input.Bind("dreapta", Keys.D, Keys.Right);

        // Forme geometrice
        Mesh cub = AddMesh(Shapes.ShadedCube());

        Mesh sfera = AddMesh(
            Shapes.ShadedSphere(1.0f, 32, 16));

        Mesh cilindru = AddMesh(
            Shapes.ShadedCylinder(0.5f, 1.0f, 24));

        Mesh grila = AddMesh(
            Shapes.Grid(10, 1.0f,
                new Color4(0.3f, 0.3f, 0.35f, 1.0f)));

        // Podeaua
        AddObject("podea", grila);

        // CAROSERIE SPORT

        // Partea principala a caroseriei
        AdaugaPiesa("caroserie", cub,
            new Vector3(0.0f, 0.5f, 0.0f),
            new Vector3(1.8f, 0.5f, 3.2f),
            new Color4(0.55f, 0.05f, 0.78f, 1.0f));

        // Partea inferioara, mov inchis
        AdaugaPiesa("partea inferioara", cub,
            new Vector3(0.0f, 0.28f, 0.0f),
            new Vector3(1.85f, 0.16f, 3.0f),
            new Color4(0.25f, 0.02f, 0.38f, 1.0f));

        // Capota
        AdaugaPiesa("capota", cub,
            new Vector3(0.0f, 0.83f, 1.0f),
            new Vector3(1.65f, 0.12f, 0.9f),
            new Color4(0.68f, 0.12f, 0.9f, 1.0f));

        // CABINA SPORT

        AdaugaPiesa("cabina", cub,
            new Vector3(0.0f, 1.0f, -0.4f),
            new Vector3(1.2f, 0.7f, 1.5f),
            new Color4(0.42f, 0.08f, 0.62f, 1.0f));

        // Geam lateral 1
        AdaugaPiesa("geam lateral fata", cub,
            new Vector3(0.615f, 1.12f, -0.05f),
            new Vector3(0.035f, 0.42f, 0.55f),
            new Color4(0.04f, 0.12f, 0.22f, 1.0f));

        // Geam lateral 2
        AdaugaPiesa("geam lateral spate", cub,
            new Vector3(0.615f, 1.12f, -0.78f),
            new Vector3(0.035f, 0.42f, 0.55f),
            new Color4(0.04f, 0.12f, 0.22f, 1.0f));

        // Geamurile de pe partea opusa
        AdaugaPiesa("geam opus fata", cub,
            new Vector3(-0.615f, 1.12f, -0.05f),
            new Vector3(0.035f, 0.42f, 0.55f),
            new Color4(0.04f, 0.12f, 0.22f, 1.0f));

        AdaugaPiesa("geam opus spate", cub,
            new Vector3(-0.615f, 1.12f, -0.78f),
            new Vector3(0.035f, 0.42f, 0.55f),
            new Color4(0.04f, 0.12f, 0.22f, 1.0f));

        // PRAGURI LATERALE

        AdaugaPiesa("prag lateral", cub,
            new Vector3(0.92f, 0.3f, 0.0f),
            new Vector3(0.12f, 0.12f, 2.3f),
            new Color4(0.3f, 0.02f, 0.45f, 1.0f));

        AdaugaPiesa("prag opus", cub,
            new Vector3(-0.92f, 0.3f, 0.0f),
            new Vector3(0.12f, 0.12f, 2.3f),
            new Color4(0.3f, 0.02f, 0.45f, 1.0f));

        // BARE DE PROTECTIE

        // Bara fata
        AdaugaPiesa("bara fata", cub,
            new Vector3(0.0f, 0.38f, 1.62f),
            new Vector3(1.9f, 0.22f, 0.22f),
            new Color4(0.25f, 0.02f, 0.35f, 1.0f));

        // Bara spate
        AdaugaPiesa("bara spate", cub,
            new Vector3(0.0f, 0.38f, -1.62f),
            new Vector3(1.9f, 0.22f, 0.22f),
            new Color4(0.25f, 0.02f, 0.35f, 1.0f));

        // FARURI SI STOPURI

        // Far fata
        AdaugaPiesa("far fata", cub,
            new Vector3(0.0f, 0.58f, 1.7f),
            new Vector3(0.65f, 0.13f, 0.06f),
            new Color4(0.7f, 0.9f, 1.0f, 1.0f));

        // Stop spate
        AdaugaPiesa("stop spate", cub,
            new Vector3(0.0f, 0.58f, -1.7f),
            new Vector3(0.65f, 0.13f, 0.06f),
            new Color4(1.0f, 0.02f, 0.04f, 1.0f));

        // SPOILER SPATE

        // Suport spoiler
        AdaugaPiesa("suport spoiler", cub,
            new Vector3(0.0f, 1.0f, -1.45f),
            new Vector3(0.12f, 0.4f, 0.12f),
            new Color4(0.15f, 0.02f, 0.2f, 1.0f));

        // Aripa spoiler
        AdaugaPiesa("spoiler", cub,
            new Vector3(0.0f, 1.25f, -1.48f),
            new Vector3(1.8f, 0.09f, 0.3f),
            new Color4(0.3f, 0.02f, 0.45f, 1.0f));

        // ROTI SI JANTE

        float[] pozitiiX = { -1.0f, 1.0f };
        float[] pozitiiZ = { -1.0f, 1.0f };

        foreach (float x in pozitiiX)
        {
            foreach (float z in pozitiiZ)
            {
                // Anvelopa
                SceneObject roata = AdaugaPiesa(
                    "anvelopa", cilindru,
                    new Vector3(x, 0.35f, z),
                    new Vector3(0.35f, 0.18f, 0.35f),
                    new Color4(0.04f, 0.04f, 0.05f, 1.0f));

                roata.Transform.Rotation =
                    new Vector3(0.0f, 0.0f, 90.0f);

                // Janta metalizata
                float directie = x > 0 ? 1.0f : -1.0f;

                SceneObject janta = AdaugaPiesa(
                    "janta", cilindru,
                    new Vector3(x + directie * 0.17f, 0.35f, z),
                    new Vector3(0.19f, 0.045f, 0.19f),
                    new Color4(0.65f, 0.68f, 0.75f, 1.0f));

                janta.Transform.Rotation =
                    new Vector3(0.0f, 0.0f, 90.0f);

                // Centrul jantei
                AdaugaPiesa("centru janta", sfera,
                    new Vector3(x + directie * 0.21f, 0.35f, z),
                    new Vector3(0.09f),
                    new Color4(0.3f, 0.3f, 0.35f, 1.0f));
            }
        }

        // SFERA CYAN

        _sfera = AddObject("sfera", sfera);

        _sfera.Transform.Scale =
            new Vector3(0.4f);

        _sfera.Tint =
            new Color4(0.0f, 1.0f, 1.0f, 1.0f);
    }

    // Metoda pentru adaugarea unei piese la masina
    private SceneObject AdaugaPiesa(
        string nume,
        Mesh mesh,
        Vector3 pozitie,
        Vector3 dimensiune,
        Color4 culoare)
    {
        SceneObject obiect = AddObject(nume, mesh);

        obiect.Transform.Position = pozitie;
        obiect.Transform.Scale = dimensiune;
        obiect.Tint = culoare;

        _masina.Add((obiect, pozitie.Z));

        return obiect;
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);

        // Deplasare continua stanga-dreapta
        if (input.IsDown("stanga"))
            _deplasareZ -= 3.0f * dt;

        if (input.IsDown("dreapta"))
            _deplasareZ += 3.0f * dt;

        _deplasareZ = MathHelper.Clamp(
            _deplasareZ, -3.0f, 3.0f);

        // Mutam toate piesele masinii
        foreach (var element in _masina)
        {
            element.Obiect.Transform.Position.Z =
                element.ZInitial + _deplasareZ;
        }

        // Rotatia verticala a sferei
        _unghi += 70.0f * dt;
        _unghi %= 360.0f;

        float radiani =
            MathHelper.DegreesToRadians(_unghi);

        float raza = 2.5f;

        float y =
            1.5f + raza * MathF.Sin(radiani);

        float z =
            _deplasareZ + raza * MathF.Cos(radiani);

        _sfera.Transform.Position =
            new Vector3(2.5f, y, z);
    }
}

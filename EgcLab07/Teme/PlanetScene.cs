
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public class PlanetScene : Scene
{
    public override string Name => "Planeta";

    private Material _planetMaterial = null!;
    private SceneObject _planet = null!;

    private Tween _seedTween = null!;
    private Tween _cloudTween = null!;

    private Vector3 _seed;
    private readonly Random _random = new Random();

    private bool _showClouds = true;

    public override string Status =>
        "G: planeta noua | Space: nori";

    public override IEnumerable<string> ActionNames
    {
        get
        {
            yield return "G - Genereaza o planeta noua";
            yield return "Space - Ascunde/afiseaza norii";
        }
    }

    protected override void Load(Input input)
    {
        CameraPosition = new Vector3(0, 0, 5);
        CameraTarget = Vector3.Zero;

        _planetMaterial = AddMaterial(
            "Planeta",
            "procedural.vert",
            "planet.frag"
        );

        Mesh sphere = AddMesh(
            Shapes.ShadedSphere(1.5f, 128, 64)
        );

        _planet = AddObject(
            "Planeta",
            sphere,
            _planetMaterial
        );

        // Samanta initiala pentru continente
        _seed = new Vector3(1.2f, 3.4f, 5.6f);

        // Parametrii reliefului
        _planetMaterial.Uniforms.Set("uSeed", _seed);
        _planetMaterial.Uniforms.Set("uLandScale", 2.8f);
        _planetMaterial.Uniforms.Set("uSeaLevel", 0.49f);
        _planetMaterial.Uniforms.Set("uCloudScale", 5.5f);
        _planetMaterial.Uniforms.Set("uCloudSpeed", 0.12f);
        _planetMaterial.Uniforms.Set("uIceLatitude", 0.82f);
        _planetMaterial.Uniforms.Set("uCloudOpacity", 1.0f);

        // Tranzitia dintre doua configuratii ale continentelor
        _seedTween = new Tween(
            _seed,
            _seed,
            2.0f,
            Easing.EaseInOut
        );

        // Tranzitia transparentei norilor
        _cloudTween = Tween.Float(
            1.0f,
            1.0f,
            0.6f,
            Easing.EaseInOut
        );

        // Rotatie continua: grade pe secunda
        _planet.Spin = new Vector3(0, 8.0f, 0);

        input.Bind("planeta noua", Keys.G);
        input.Bind("nori", Keys.Space);
    }

    public override void Update(float dt, Input input)
    {
        // Actualizeaza rotatia planetei
        base.Update(dt, input);

        // Actualizeaza samanta continentelor
        _seedTween.Update(dt);
        _seed = _seedTween.Value;

        _planetMaterial.Uniforms.Set("uSeed", _seed);

        // Actualizeaza transparenta norilor
        _cloudTween.Update(dt);

        _planetMaterial.Uniforms.Set(
            "uCloudOpacity",
            _cloudTween.Scalar
        );

        // Genereaza o noua planeta
        if (input.WasPressed("planeta noua"))
        {
            Vector3 newSeed = new Vector3(
                (float)_random.NextDouble() * 20.0f,
                (float)_random.NextDouble() * 20.0f,
                (float)_random.NextDouble() * 20.0f
            );

            _seedTween.GoTo(newSeed, 3.0f);
        }

        // Ascunde sau afiseaza norii progresiv
        if (input.WasPressed("nori"))
        {
            _showClouds = !_showClouds;

            _cloudTween.GoTo(
                _showClouds ? 1.0f : 0.0f,
                0.7f
            );
        }
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe())
            yield return line;

        yield return "Planeta procedurala";
        yield return "Oceane, continente, paduri si munti";
        yield return "Calote polare si nori animati";
        yield return "G: genereaza alta planeta";
        yield return "Space: ascunde/afiseaza norii";
        yield return "Rotatie continua";
    }
}

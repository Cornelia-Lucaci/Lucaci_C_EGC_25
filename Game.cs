using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Fereastra aplicației și bucla principală (main game loop).
/// Față de laboratorul 3: o scenă cu mai multe obiecte, fiecare cu matricea lui model,
/// o cameră cu matricele view și projection, tastatura și mouse-ul.
/// </summary>
public class Game : GameWindow
{
    // Obiecte care NU depind de OpenGL pot fi create imediat.
    private readonly Camera _camera = new();

    // Obiectele care folosesc OpenGL se creează în OnLoad (abia atunci există contextul OpenGL).
    private Shader _shader = null!;
    private readonly List<SceneObject> _scene = new();
    private readonly List<Transform2D> _initialTransforms = new();   // pentru tasta R

    // Starea aplicației (se modifică în OnUpdateFrame, se folosește în OnRenderFrame).
    private int _selected;            // indexul obiectului selectat în _scene (tasta Tab)
    private bool _wireframe;          // contur (true) sau plin (false)

    // T4: Pivotul în jurul căruia se rotește obiectul.
    private Vector2 _pivot;
    private bool _hasPivot = false;

    //T5
    private float _satelliteAngle = 0.0f;
    private const float SatelliteDistance = 1.2f;
    private const float SatelliteScale = 0.3f;
    private Mesh _satelliteMesh = null!;

    // Contoare afișate în titlul ferestrei.
    private readonly string _baseTitle;
    private double _secondTimer;
    private int _frames, _lastFrames;
    private bool _titleDirty = true;

    public Game(int width, int height, string title)
        : base(GameWindowSettings.Default,
               new NativeWindowSettings
               {
                   ClientSize = new Vector2i(width, height),
                   Title = title,
                   APIVersion = new Version(3, 3),          // cerem OpenGL 3.3 ...
                   Profile = ContextProfile.Core,           // ... profil Core (fără modul imediat)
                   Flags = ContextFlags.ForwardCompatible
               })
    {
        _baseTitle = title;
        VSync = VSyncMode.On;   // swap-ul bufferelor așteaptă reîmprospătarea monitorului
    }

    /// <summary>Apelată O SINGURĂ DATĂ, după crearea ferestrei și a contextului OpenGL.</summary>
    protected override void OnLoad()
    {
        base.OnLoad();

        Console.WriteLine($"OpenGL  : {GL.GetString(StringName.Version)}");
        Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _camera.Resize(ClientSize.X, ClientSize.Y);

        _shader = Shader.FromFiles("Shaders/shader.vert", "Shaders/shader.frag");

        // T5: creăm triunghiul care va reprezenta satelitul.
        _satelliteMesh = Shapes.Triangle();

        // Scena: aceleași forme din laboratorul 3, acum plasate în lume prin Transform2D.
        // Depth (z) contează doar în perspectivă (tasta O, secțiunea 7.2).
        AddObject("pătrat", Shapes.Quad(),
                  new Transform2D { Position = new Vector2(-1.8f, -1.0f), Depth = -3.0f });
        AddObject("triunghi", Shapes.Triangle(),
                  new Transform2D { Position = new Vector2(-1.2f, 1.0f) });
        AddObject("hexagon", Shapes.RegularPolygon(6, 0.5f),
                  new Transform2D { Position = new Vector2(2.0f, 1.0f) });
        AddObject("cerc", Shapes.RegularPolygon(64, 0.5f),
                  new Transform2D { Position = new Vector2(0.8f, -0.6f), Depth = 2.0f });
    }

    /// <summary>Adaugă un obiect în scenă și reține transformarea lui inițială (pentru tasta R).</summary>
    private void AddObject(string name, Mesh mesh, Transform2D transform)
    {
        _scene.Add(new SceneObject(name, mesh, transform));
        _initialTransforms.Add(transform.Clone());
    }

    /// <summary>Apelată când se schimbă dimensiunea ferestrei.</summary>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);   // zona ferestrei în care se desenează
        _camera.Resize(e.Width, e.Height);      // proiecția se adaptează la noul raport de aspect
    }

    /// <summary>Apelată la fiecare iterație a buclei: LOGICA aplicației (intrări, mișcare, timp).</summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        // --- Obiectul selectat: tastatura (secțiunea 8) ------------------------------

        float dt = (float)args.Time;

        // T5: satelitul parcurge o orbită completă
        _satelliteAngle += dt * MathF.PI / 2.0f;
        if (_satelliteAngle >= MathF.Tau)
            _satelliteAngle -= MathF.Tau;

        SceneObject sel = _scene[_selected];

        const float moveSpeed = 2.0f;   // unități de lume pe secundă
        /*if (KeyboardState.IsKeyDown(Keys.Left))  sel.Transform.Position.X -= moveSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.Right)) sel.Transform.Position.X += moveSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.Down))  sel.Transform.Position.Y -= moveSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.Up))    sel.Transform.Position.Y += moveSpeed * dt;*/

        // TODO (T2): Q / E rotesc obiectul, Z / X îl scalează

        float speedMultiplier = KeyboardState.IsKeyDown(Keys.LeftShift) ? 3.0f : 1.0f;

        // Deplasare cu săgețile.
        if (KeyboardState.IsKeyDown(Keys.Left))
            sel.Transform.Position.X -= moveSpeed * speedMultiplier * dt;
        if (KeyboardState.IsKeyDown(Keys.Right))
            sel.Transform.Position.X += moveSpeed * speedMultiplier * dt;
        if (KeyboardState.IsKeyDown(Keys.Down))
            sel.Transform.Position.Y -= moveSpeed * speedMultiplier * dt;
        if (KeyboardState.IsKeyDown(Keys.Up))
            sel.Transform.Position.Y += moveSpeed * speedMultiplier * dt;

        // Q și E: rotație cu 90 de grade pe secundă.
        if (KeyboardState.IsKeyDown(Keys.Q))
            sel.Transform.Rotation += 90.0f * speedMultiplier * dt;
        if (KeyboardState.IsKeyDown(Keys.E))
            sel.Transform.Rotation -= 90.0f * speedMultiplier * dt;

        // Z și X: micșorare și mărire uniformă.
        float scaleFactor = MathF.Pow(1.5f, dt * speedMultiplier);
        if (KeyboardState.IsKeyDown(Keys.Z))
            sel.Transform.Scale /= scaleFactor;
        if (KeyboardState.IsKeyDown(Keys.X))
            sel.Transform.Scale *= scaleFactor;

        // Limităm dimensiunea obiectului.
        sel.Transform.Scale.X = Math.Clamp(sel.Transform.Scale.X, 0.1f, 10.0f);
        sel.Transform.Scale.Y = Math.Clamp(sel.Transform.Scale.Y, 0.1f, 10.0f);

        // Scalarea se face prin înmulțire deoarece modifică proporțional dimensiunea actuală a obiectului.
        // Prin adunare, s-ar adăuga o valoare fixă, indiferent de dimensiunea obiectului.

        if (KeyboardState.IsKeyPressed(Keys.Tab)) _selected = (_selected + 1) % _scene.Count;
        if (KeyboardState.IsKeyPressed(Keys.O))   _camera.IsPerspective = !_camera.IsPerspective;
        if (KeyboardState.IsKeyPressed(Keys.F))   _wireframe = !_wireframe;
        if (KeyboardState.IsKeyPressed(Keys.R))   ResetScene();

        if (KeyboardState.IsKeyPressed(Keys.Tab) || KeyboardState.IsKeyPressed(Keys.O) ||
            KeyboardState.IsKeyPressed(Keys.R))
        {
            _titleDirty = true;
        }

        // --- Obiectul selectat: mouse-ul (secțiunea 9) -------------------------------

        // TODO (T3): la clic stânga, obiectul selectat se mută în punctul din lume aflat sub cursor
        //            (folosiți ScreenToWorld; Depth nu se schimbă)

        if (MouseState.IsButtonPressed(MouseButton.Left))
        {
            Vector2 worldPosition = ScreenToWorld(MouseState.Position);

            _scene[_selected].Transform.Position.X = worldPosition.X;
            _scene[_selected].Transform.Position.Y = worldPosition.Y;
        }

        // TODO (T4, facultativ): clic dreapta = pivot; tasta P ținută = rotație în jurul pivotului

        if (MouseState.IsButtonPressed(MouseButton.Right))
        {
            _pivot = ScreenToWorld(MouseState.Position);
            _hasPivot = true;
        }

        if (KeyboardState.IsKeyDown(Keys.P) && _hasPivot)
        {
            SceneObject obj = _scene[_selected];

            float angle = MathHelper.DegreesToRadians(90.0f * dt);

            // Poziția obiectului relativă la pivot.
            Vector2 offset = obj.Transform.Position - _pivot;

            // Rotim poziția în jurul pivotului.
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);

            Vector2 rotatedOffset = new Vector2(
                offset.X * cos - offset.Y * sin,
                offset.X * sin + offset.Y * cos
            );

            obj.Transform.Position = _pivot + rotatedOffset;

            // Rotim și obiectul propriu-zis cu același unghi.
            obj.Transform.Rotation += 90.0f * dt;
        }

        // --- Camera: tastatura și rotița (secțiunea 6.2) -----------------------------

        float camSpeed = 3.0f / _camera.Zoom;                    // unități de lume pe secundă
        if (KeyboardState.IsKeyDown(Keys.A)) _camera.Position.X -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.D)) _camera.Position.X += camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.S)) _camera.Position.Y -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.W)) _camera.Position.Y += camSpeed * dt;

        _camera.Zoom *= MathF.Pow(1.1f, MouseState.ScrollDelta.Y);   // un pas al rotiței = ±10 %
        _camera.Zoom = Math.Clamp(_camera.Zoom, 0.1f, 10.0f);

        // --- Titlul ferestrei: obiectul selectat, proiecția, cadre pe secundă --------

        _secondTimer += args.Time;
        if (_secondTimer >= 1.0)
        {
            _lastFrames = _frames;
            _frames = 0;
            _secondTimer -= 1.0;
            _titleDirty = true;
        }

        if (_titleDirty)
        {
            string projection = _camera.IsPerspective ? "perspectivă" : "ortografică";
            Title = $"{_baseTitle} | selectat: {_scene[_selected].Name} | proiecție {projection} | FPS: {_lastFrames}";
            _titleDirty = false;
        }
    }

    /// <summary>Apelată la fiecare iterație a buclei: DESENAREA unui cadru.</summary>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _frames++;

        // 1. Ștergem imaginea veche din back buffer.
        GL.ClearColor(0.12f, 0.14f, 0.18f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // 2. Starea de rasterizare și programul shader.
        SetWireframe(_wireframe);
        _shader.Use();

        // 3. View și projection sunt aceleași pentru toate obiectele: le trimitem o singură dată.
        _shader.SetMatrix4("uView", _camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", _camera.ProjectionMatrix);

        // 4. Fiecare obiect: matricea lui model, nuanța, apoi desenarea.
        foreach (SceneObject obj in _scene)
        {
            _shader.SetMatrix4("uModel", obj.Transform.ModelMatrix);
            _shader.SetColor("uTint", obj.Tint);
            obj.Mesh.Draw();
        }

        // TODO (T5, facultativ): satelitul obiectului selectat

        _satelliteAngle += (float)args.Time * MathF.PI / 2.0f;

        if (_satelliteAngle >= MathF.Tau)
            _satelliteAngle -= MathF.Tau;

        Matrix4 satelliteModel =
        Matrix4.CreateScale(SatelliteScale) *
        Matrix4.CreateTranslation(SatelliteDistance, 0.0f, 0.0f) *
        Matrix4.CreateRotationZ(_satelliteAngle) *
        _scene[_selected].Transform.ModelMatrix;

        // Când obiectul selectat este scalat, distanța efectivă a satelitului
        // față de centrul obiectului se modifică. Acest lucru se întâmplă deoarece
        // matricea model a obiectului selectat este înmulțită la final, astfel încât
        // scalarea obiectului afectează și translația locală a satelitului.

        _shader.SetMatrix4("uModel", satelliteModel);
        _shader.SetColor("uTint", new Color4(1.0f, 0.8f, 0.2f, 1.0f));
        _satelliteMesh.Draw();

        // 5. Afișăm cadrul terminat: back buffer <-> front buffer.
        SwapBuffers();
    }

    /// <summary>Apelată O SINGURĂ DATĂ, la închiderea ferestrei: eliberăm resursele GPU.</summary>
    protected override void OnUnload()
    {
        foreach (SceneObject obj in _scene)
        {
            obj.Dispose();
        }
        _shader.Dispose();

        base.OnUnload();
    }

    /// <summary>Transformă poziția mouse-ului (pixeli, origine stânga-sus) în coordonate de lume.</summary>
    private Vector2 ScreenToWorld(Vector2 mousePixels)
    {
        // TODO (T3): pasul 1 (pixel → NDC), apoi pasul 2 (NDC → lume, cu inversa lui view * projection)
        
        //pasul 1
        float x = 2.0f * mousePixels.X / ClientSize.X - 1.0f;
        // Semnul minus (1 - ...) inversează axa Y, deoarece coordonatele pixelilor
        // cresc de sus în jos, în timp ce coordonatele NDC cresc de jos în sus.

        float y = 1.0f - 2.0f * mousePixels.Y / ClientSize.Y;

        //pasul 2
        Vector4 ndc = new Vector4(x, y, 0.0f, 1.0f);

        Matrix4 inverseViewProjection =
            Matrix4.Invert(_camera.ViewMatrix * _camera.ProjectionMatrix);

        Vector4 world = ndc * inverseViewProjection;

        return new Vector2(
            world.X / world.W,
            world.Y / world.W
        );
    }

    /// <summary>Tasta R: obiectele revin la transformările inițiale, camera la starea inițială.</summary>
    private void ResetScene()
    {
        for (int i = 0; i < _scene.Count; i++)
        {
            _scene[i].Transform = _initialTransforms[i].Clone();
        }
        _camera.Reset();
    }

    /// <summary>
    /// Contur / plin. Dacă Visual Studio semnalează o eroare la primul parametru,
    /// folosiți aici tipul acceptat de versiunea voastră (vezi laboratorul 2, Problema 3):
    /// TriangleFace.FrontAndBack sau MaterialFace.FrontAndBack.
    /// </summary>
    private static void SetWireframe(bool wireframe)
    {
        GL.PolygonMode(TriangleFace.FrontAndBack, wireframe ? PolygonMode.Line : PolygonMode.Fill);
    }
}

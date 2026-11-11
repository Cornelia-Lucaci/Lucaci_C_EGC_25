using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc.Teme;

/// <summary>
/// Tema B: Evită obstacolele (Endless Runner pe 3 benzi).
/// - Stânga / Dreapta: schimbare lină de bandă cu Tween (EaseOut, 0.15 s).
/// - Space: salt cu gravitație peste obstacolele joase.
/// - Obstacole joase (se pot sări) și înalte (se ocolesc), generate cu frecvență și viteză crescătoare.
/// - Coliziune AABB (cutii aliniate cu axele) pe toate cele 3 axe.
/// - Bonus: marcaje rutiere care se deplasează odată cu lumea.
/// </summary>
public sealed class DodgeObstaclesScene : Scene
{
    private const string RestartAction = "joc nou";

    // Configurația drumului și a benzilor (x = -2.2, 0.0, +2.2)
    private static readonly float[] LaneX = { -2.2f, 0.0f, 2.2f };
    private const float PlayerZ = 3.5f;
    private const float PlayerHalfWidth = 0.4f;
    private const float PlayerHalfHeight = 0.45f;
    private const float PlayerHalfDepth = 0.4f;
    private const float GroundY = PlayerHalfHeight;

    // Fizica săriturii
    private const float JumpSpeed = 9.5f;
    private const float Gravity = 24.0f;

    // Dificultate progresivă
    private const float InitialWorldSpeed = 10.0f;
    private const float SpeedAcceleration = 0.45f; // unități/s²
    private const float SpawnZ = -38.0f;
    private const float DespawnZ = 10.0f;

    private Mesh _cubeMesh = null!;
    private Mesh _diskMesh = null!;

    // Jucător
    private SceneObject _playerObj = null!;
    private SceneObject _shadowObj = null!;
    private int _currentLane = 1; // 0 = stânga, 1 = centru, 2 = dreapta
    private Tween _laneTween = null!;
    private float _playerY = GroundY;
    private float _verticalSpeed;

    // Obstacole active
    private sealed class Obstacle
    {
        public SceneObject Obj = null!;
        public Vector3 HalfExtents;
    }
    private readonly List<Obstacle> _obstacles = new();

    // Bonus: marcaje de drum
    private readonly List<SceneObject> _roadStripes = new();
    private const float StripeSpacing = 4.0f;

    // Stare joc și scor
    private readonly Random _rng = new();
    private float _worldSpeed;
    private float _spawnTimer;
    private float _score;
    private float _highScore;
    private bool _gameOver;

    public override string Name => "evită obstacolele";

    public override IEnumerable<string> ActionNames => new[]
    {
        Actions.Left, Actions.Right, Actions.Jump, RestartAction
    };

    public override string Status => _gameOver
        ? $"GAME OVER | Scor: {_score:F1} s | Record: {_highScore:F1} s (R = joc nou)"
        : $"Scor: {_score:F1} s | Record: {_highScore:F1} s | Viteză: {_worldSpeed:F1}";

    public override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 4.8f, 9.5f);
        CameraTarget = new Vector3(0.0f, 1.0f, -4.0f);

        // Legăm acțiunile (inclusiv tastele suplimentare pentru Reset)
        input.Bind(Actions.Left, Keys.A, Keys.Left);
        input.Bind(Actions.Right, Keys.D, Keys.Right);
        input.Bind(Actions.Jump, Keys.Space, Keys.W, Keys.Up);
        input.Bind(RestartAction, Keys.R);

        _cubeMesh = AddMesh(Shapes.ShadedCube());
        _diskMesh = AddMesh(Shapes.ShadedCylinder(0.5f, 0.02f, 24));

        BuildRoad();
        BuildPlayer();
        ResetGame();
    }

    private void BuildRoad()
    {
        // Asfaltul (drumul principal pe cele 3 benzi)
        SceneObject road = AddObject("drum", _cubeMesh);
        road.Transform.Position = new Vector3(0.0f, -0.05f, -15.0f);
        road.Transform.Scale = new Vector3(7.2f, 0.1f, 55.0f);
        road.Tint = new Color4(0.18f, 0.20f, 0.24f, 1.0f);

        // Borduri laterale
        foreach (float side in new[] { -3.75f, 3.75f })
        {
            SceneObject curb = AddObject("bordură", _cubeMesh);
            curb.Transform.Position = new Vector3(side, 0.15f, -15.0f);
            curb.Transform.Scale = new Vector3(0.3f, 0.3f, 55.0f);
            curb.Tint = new Color4(0.85f, 0.25f, 0.25f, 1.0f);
        }

        // Bonus: linii întrerupte între benzi (la x = -1.1 și x = +1.1)
        for (float z = SpawnZ; z <= DespawnZ; z += StripeSpacing)
        {
            foreach (float dividerX in new[] { -1.1f, 1.1f })
            {
                SceneObject stripe = AddObject("marcaj", _cubeMesh);
                stripe.Transform.Position = new Vector3(dividerX, 0.01f, z);
                stripe.Transform.Scale = new Vector3(0.12f, 0.02f, 1.8f);
                stripe.Tint = new Color4(0.92f, 0.92f, 0.95f, 1.0f);
                _roadStripes.Add(stripe);
            }
        }
    }

    private void BuildPlayer()
    {
        _shadowObj = AddObject("umbră jucător", _diskMesh);
        _shadowObj.Tint = new Color4(0.05f, 0.05f, 0.08f, 1.0f);

        _playerObj = AddObject("jucător", _cubeMesh);
        _playerObj.Transform.Scale = new Vector3(PlayerHalfWidth * 2f, PlayerHalfHeight * 2f, PlayerHalfDepth * 2f);
        _playerObj.Tint = new Color4(0.25f, 0.85f, 1.0f, 1.0f);

        _laneTween = new Tween(
            new Vector3(LaneX[1], 0f, 0f),
            new Vector3(LaneX[1], 0f, 0f),
            0.15f,
            Easing.EaseOut
        );
    }

    private void ResetGame()
    {
        // Eliminăm obstacolele din runda anterioară
        foreach (Obstacle obs in _obstacles)
        {
            Objects.Remove(obs.Obj);
        }
        _obstacles.Clear();

        _currentLane = 1;
        _laneTween = new Tween(
            new Vector3(LaneX[_currentLane], 0f, 0f),
            new Vector3(LaneX[_currentLane], 0f, 0f),
            0.15f,
            Easing.EaseOut
        );

        _playerY = GroundY;
        _verticalSpeed = 0.0f;
        _playerObj.Tint = new Color4(0.25f, 0.85f, 1.0f, 1.0f);

        _worldSpeed = InitialWorldSpeed;
        _spawnTimer = 0.6f;
        _score = 0.0f;
        _gameOver = false;

        UpdatePlayerVisuals();
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);

        // Repornire joc la tasta R
        if (input.WasPressed(RestartAction))
        {
            ResetGame();
            ConsolePanel.PrintMessage("joc nou pornit");
            return;
        }

        // Dacă jocul s-a oprit la coliziune, înghețăm logica până la R
        if (_gameOver || dt <= 0.0f) return;

        // 1. Actualizare scor și creșterea vitezei lumii în timp
        _score += dt;
        _worldSpeed += SpeedAcceleration * dt;

        // 2. Schimbarea benzii (Tween cu EaseOut, 0.15 s < 0.2 s)
        if (input.WasPressed(Actions.Left) && _currentLane > 0)
        {
            _currentLane--;
            _laneTween.GoTo(new Vector3(LaneX[_currentLane], 0f, 0f), 0.15f);
        }
        if (input.WasPressed(Actions.Right) && _currentLane < LaneX.Length - 1)
        {
            _currentLane++;
            _laneTween.GoTo(new Vector3(LaneX[_currentLane], 0f, 0f), 0.15f);
        }
        _laneTween.Update(dt);

        // 3. Salt cu gravitație (secțiunea 5.4)
        bool onGround = _playerY <= GroundY + 1e-4f;
        if (onGround && input.WasPressed(Actions.Jump))
        {
            _verticalSpeed = JumpSpeed;
        }

        if (!onGround || _verticalSpeed > 0.0f)
        {
            _verticalSpeed -= Gravity * dt;
            _playerY += _verticalSpeed * dt;
            if (_playerY <= GroundY)
            {
                _playerY = GroundY;
                _verticalSpeed = 0.0f;
            }
        }

        UpdatePlayerVisuals();

        // 4. Bonus: deplasarea marcajelor pe drum pentru senzația de viteză
        float trackLength = DespawnZ - SpawnZ;
        foreach (SceneObject stripe in _roadStripes)
        {
            Vector3 pos = stripe.Transform.Position;
            pos.Z += _worldSpeed * dt;
            if (pos.Z > DespawnZ)
            {
                pos.Z -= trackLength;
            }
            stripe.Transform.Position = pos;
        }

        // 5. Generarea obstacolelor (frecvența crește odată cu viteza)
        _spawnTimer -= dt;
        if (_spawnTimer <= 0.0f)
        {
            SpawnWave();
            // Intervalul scade pe măsură ce viteza crește (de la ~1.25 s spre ~0.55 s)
            float baseInterval = MathF.Max(0.55f, 13.0f / _worldSpeed);
            _spawnTimer = baseInterval * (0.85f + (float)_rng.NextDouble() * 0.3f);
        }

        // 6. Deplasarea obstacolelor și verificarea coliziunilor AABB
        Vector3 playerCenter = _playerObj.Transform.Position;
        Vector3 playerHalf = new(PlayerHalfWidth, PlayerHalfHeight, PlayerHalfDepth);

        for (int i = _obstacles.Count - 1; i >= 0; i--)
        {
            Obstacle obs = _obstacles[i];
            Vector3 pos = obs.Obj.Transform.Position;
            pos.Z += _worldSpeed * dt;
            obs.Obj.Transform.Position = pos;

            // Test coliziune AABB pe toate cele 3 axe
            if (CheckAABB(playerCenter, playerHalf, pos, obs.HalfExtents))
            {
                _gameOver = true;
                if (_score > _highScore) _highScore = _score;
                _playerObj.Tint = new Color4(1.0f, 0.2f, 0.2f, 1.0f);
                ConsolePanel.PrintMessage($"COLIZIUNE! Scor: {_score:F1} s | Record: {_highScore:F1} s (apasă R)");
                return;
            }

            // Eliminare obstacole trecute de cameră
            if (pos.Z > DespawnZ)
            {
                Objects.Remove(obs.Obj);
                _obstacles.RemoveAt(i);
            }
        }
    }

    private void SpawnWave()
    {
        // Alegem 1 sau 2 benzi pe care punem obstacole (lăsând mereu cel puțin o bandă liberă)
        int freeLane = _rng.Next(0, 3);
        bool spawnTwo = _rng.NextDouble() < 0.45;
        int secondLane = (freeLane + 1 + _rng.Next(0, 2)) % 3;

        for (int lane = 0; lane < 3; lane++)
        {
            if (lane == freeLane) continue;
            if (!spawnTwo && lane != secondLane) continue;

            bool isTall = _rng.NextDouble() < 0.45;
            CreateObstacle(lane, isTall);
        }
    }

    private void CreateObstacle(int lane, bool isTall)
    {
        // Obstacol jos: înălțime 0.8 (poate fi sărit, deoarece vârful saltului e ~1.88)
        // Obstacol înalt: înălțime 2.6 (nu poate fi sărit, trebuie ocolit pe altă bandă)
        float halfW = 0.75f;
        float halfH = isTall ? 1.3f : 0.4f;
        float halfD = 0.45f;

        SceneObject obj = AddObject(isTall ? "obstacol înalt" : "obstacol jos", _cubeMesh);
        obj.Transform.Position = new Vector3(LaneX[lane], halfH, SpawnZ);
        obj.Transform.Scale = new Vector3(halfW * 2f, halfH * 2f, halfD * 2f);
        obj.Tint = isTall
            ? new Color4(0.95f, 0.30f, 0.25f, 1.0f)  // roșu: se ocolește
            : new Color4(1.0f, 0.75f, 0.15f, 1.0f);  // galben-portocaliu: se sare

        _obstacles.Add(new Obstacle
        {
            Obj = obj,
            HalfExtents = new Vector3(halfW, halfH, halfD)
        });
    }

    private void UpdatePlayerVisuals()
    {
        float x = _laneTween.Value.X;
        _playerObj.Transform.Position = new Vector3(x, _playerY, PlayerZ);

        // Umbra rămâne pe asfalt și se micșorează în timpul saltului
        float heightAboveGround = _playerY - GroundY;
        float shadowScale = 1.05f / (1.0f + 0.35f * heightAboveGround);
        _shadowObj.Transform.Position = new Vector3(x, 0.01f, PlayerZ);
        _shadowObj.Transform.Scale = new Vector3(shadowScale, 1.0f, shadowScale);
    }

    /// <summary>
    /// Coliziune AABB (Axis-Aligned Bounding Box): două cutii aliniate cu axele
    /// se suprapun dacă și numai dacă se suprapun pe toate cele trei axe (X, Y și Z).
    /// </summary>
    private static bool CheckAABB(Vector3 centerA, Vector3 halfA, Vector3 centerB, Vector3 halfB)
    {
        bool overlapX = MathF.Abs(centerA.X - centerB.X) <= (halfA.X + halfB.X);
        bool overlapY = MathF.Abs(centerA.Y - centerB.Y) <= (halfA.Y + halfB.Y);
        bool overlapZ = MathF.Abs(centerA.Z - centerB.Z) <= (halfA.Z + halfB.Z);
        return overlapX && overlapY && overlapZ;
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        yield return $"Stare joc      : {(_gameOver ? "GAME OVER" : "activ")} | Scor: {_score:F2} s | Record: {_highScore:F2} s";
        yield return $"Viteză lume    : {_worldSpeed:F2} u/s | Obstacole active: {_obstacles.Count}";
    }
}
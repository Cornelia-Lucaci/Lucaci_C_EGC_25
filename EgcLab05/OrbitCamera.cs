using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Camera orbită: privește mereu spre un punct țintă (Target) și se deplasează
/// pe o sferă în jurul lui, controlată prin distanță, azimut și elevație.
/// </summary>
public class OrbitCamera : Camera
{
    // --- Starea camerei ---------------------------------------------------------

    public Vector3 Target { get; set; }
    public float Distance { get; private set; }
    public float Azimuth { get; private set; }     // în grade (în jurul axei Y)
    public float Elevation { get; private set; }   // în grade (deasupra planului orizontal)

    private Vector3 _position;
    public override Vector3 Position => _position;
    public override Vector3 Front => Vector3.Normalize(Target - _position);

    // --- Parametri ----------------------------------------------------------------

    public const float MaxElevation = 89.0f;
    public const float MinDistance = 1.5f;
    public const float MaxDistance = 25.0f;

    public float MouseSensitivity = 0.3f;
    public float KeyRotateSpeed = 90.0f;    // grade pe secundă (săgeți)
    public float ZoomStep = 0.8f;           // unități pe pas al rotiței
    public float TargetMoveSpeed = 4.0f;    // bonus 2: viteza țintei cu W A S D

    private static readonly Vector3 DefaultTarget = new(0.0f, 1.0f, -2.0f);
    private const float DefaultDistance = 8.0f;
    private const float DefaultAzimuth = 90.0f;
    private const float DefaultElevation = 20.0f;

    public OrbitCamera()
    {
        Reset();
    }

    public override string Name => "Orbită";

    public override Matrix4 ViewMatrix =>
        Matrix4.LookAt(_position, Target, Vector3.UnitY);

    public override void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt)
    {
        // 1. Rotație din mouse (doar când clic stânga este ținut apăsat)
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            Azimuth += mouseDelta.X * MouseSensitivity;
            Elevation += mouseDelta.Y * MouseSensitivity;
        }

        // 2. Rotație din tastatură (săgețile)
        if (keyboard.IsKeyDown(Keys.Left)) Azimuth -= KeyRotateSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Right)) Azimuth += KeyRotateSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Up)) Elevation += KeyRotateSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Down)) Elevation -= KeyRotateSpeed * dt;

        Elevation = Math.Clamp(Elevation, -MaxElevation, MaxElevation);

        // 3. Zoom din rotița mouse-ului (modifică distanța, NU FOV-ul)
        if (mouse.ScrollDelta.Y != 0f)
        {
            Distance = Math.Clamp(Distance - mouse.ScrollDelta.Y * ZoomStep, MinDistance, MaxDistance);
        }

        // 4. BONUS 2: W A S D mută ținta (Target) în planul orizontal XZ
        Vector3 flatForward = new(Front.X, 0f, Front.Z);
        if (flatForward.LengthSquared > 0.0001f)
            flatForward = Vector3.Normalize(flatForward);
        else
            flatForward = -Vector3.UnitZ;

        Vector3 flatRight = Vector3.Normalize(Vector3.Cross(flatForward, Vector3.UnitY));

        Vector3 targetMove = Vector3.Zero;
        if (keyboard.IsKeyDown(Keys.W)) targetMove += flatForward;
        if (keyboard.IsKeyDown(Keys.S)) targetMove -= flatForward;
        if (keyboard.IsKeyDown(Keys.D)) targetMove += flatRight;
        if (keyboard.IsKeyDown(Keys.A)) targetMove -= flatRight;

        if (targetMove.LengthSquared > 0f)
        {
            Target += Vector3.Normalize(targetMove) * TargetMoveSpeed * dt;
        }

        UpdatePosition();
    }

    /// <summary>
    /// BONUS 1 (FPS -> Orbită): poziționează camera orbită astfel încât să continue exact
    /// cadrul camerei FPS fără salt vizual, plasând ținta la o distanță fixă în fața ei.
    /// </summary>
    public void SyncFromFps(Vector3 fpsPosition, Vector3 fpsFront, float distance = 5.0f)
    {
        Vector3 dir = Vector3.Normalize(fpsFront);
        Target = fpsPosition + dir * distance;
        Distance = Math.Clamp(distance, MinDistance, MaxDistance);

        // Vectorul unitar țintă -> cameră este o = -front
        Vector3 o = -dir;
        float elRad = MathF.Asin(Math.Clamp(o.Y, -1.0f, 1.0f));
        float azRad = MathF.Atan2(o.Z, o.X);

        Elevation = Math.Clamp(MathHelper.RadiansToDegrees(elRad), -MaxElevation, MaxElevation);
        Azimuth = MathHelper.RadiansToDegrees(azRad);

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        float azRad = MathHelper.DegreesToRadians(Azimuth);
        float elRad = MathHelper.DegreesToRadians(Elevation);

        Vector3 offset = new(
            MathF.Cos(elRad) * MathF.Cos(azRad),
            MathF.Sin(elRad),
            MathF.Cos(elRad) * MathF.Sin(azRad)
        );

        _position = Target + Distance * offset;
    }

    public override void Reset()
    {
        Target = DefaultTarget;
        Distance = DefaultDistance;
        Azimuth = DefaultAzimuth;
        Elevation = DefaultElevation;
        FieldOfView = 60.0f;
        UpdatePosition();
    }

    public override IEnumerable<string> Describe()
    {
        yield return $"Țintă (Target) : ({Target.X,6:F2}, {Target.Y,6:F2}, {Target.Z,6:F2})";
        yield return $"Distanță       : {Distance,6:F2}";
        yield return $"Azimut / elev. : {NormalizeAngle(Azimuth),7:F1}° / {Elevation,6:F1}°";
        yield return $"Poziție        : ({_position.X,6:F2}, {_position.Y,6:F2}, {_position.Z,6:F2})";
    }

    private static float NormalizeAngle(float degrees)
    {
        degrees %= 360.0f;
        if (degrees > 180.0f) degrees -= 360.0f;
        if (degrees <= -180.0f) degrees += 360.0f;
        return degrees;
    }
}
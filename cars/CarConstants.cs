/// <summary>
/// All car tuning values in one place. Pure C# — no Godot dependency.
/// </summary>
public static class CarConstants
{
    // Chassis
    public const float ChassisWidth = 1.0f;
    public const float ChassisHeight = 0.3f;
    public const float ChassisLength = 1.8f;
    public const float ChassisMass = 80f;

    // Wheel (visual only — no separate physics body)
    public const float WheelRadius = 0.25f;
    public const float WheelWidth = 0.15f;

    // Wheel positions relative to chassis center (x = left/right, z = front/back)
    public const float WheelBaseHalf = 0.7f;   // half length between front/rear axles
    public const float TrackHalf = 0.45f;       // half width between left/right wheels

    // Suspension spring
    public const float SpringStiffness = 1500f;   // N/m
    public const float SpringDamping = 100f;        // Ns/m
    public const float SpringRestLength = 0.5f;    // m
    public const float RayLength = 0.9f;           // m (rest + wheel radius + extra travel)
    public const float MaxSpringForce = 2000f;     // N clamp

    // Drive
    public const float ThrottleForce = 400f;       // N
    public const float BrakeForce = 300f;           // N
    public const float ReverseForce = 150f;         // N (half-power reverse)
    public const float SteeringSpeed = 2.5f;        // rad/s target angular velocity when steering

    // Camera
    public const float CameraBehind = 8f;
    public const float CameraAbove = 4f;
    public const float CameraSmoothing = 4f;        // lerp speed
}

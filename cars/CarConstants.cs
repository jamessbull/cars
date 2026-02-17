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
    public const float WheelRadius = 0.35f;
    public const float WheelWidth = 0.3f;

    // Wheel positions relative to chassis center (x = left/right, z = front/back)
    public const float WheelBaseHalf = 0.95f;   // half length between front/rear axles
    public const float TrackHalf = 0.55f;       // half width between left/right wheels

    // Suspension spring
    public const float SpringStiffness = 1500f;   // N/m
    public const float SpringDamping = 100f;        // Ns/m
    public const float SpringRestLength = 0.5f;    // m
    public const float RayLength = 0.9f;           // m (rest + wheel radius + extra travel)
    public const float MaxSpringForce = 2000f;     // N clamp

    // Drive
    public const float ThrottleForce = 450f;       // N
    public const float BrakeForce = 300f;           // N
    public const float ReverseForce = 150f;         // N (half-power reverse)

    // Steering
    public const float MaxSteerAngle = 0.7854f;    // rad (45 degrees)
    public const int SteerSteps = 10;               // frames to reach full lock
    public const float SteerAngleStep = MaxSteerAngle / SteerSteps; // rad per frame
    public const float Wheelbase = WheelBaseHalf * 2f; // distance between front and rear axles
    public const float MaxYawSpeed = 2.5f;              // rad/s — caps turning rate regardless of speed

    // Surface
    public const float RollingDragFactor = 0.995f; // velocity multiplier per frame (mild rolling resistance)
    public const float GravelDragFactor = 0.95f;   // velocity multiplier per frame on gravel


    // Camera
    public const float CameraBehind = 8f;
    public const float CameraAbove = 4f;
    public const float CameraSmoothing = 4f;        // lerp speed
}

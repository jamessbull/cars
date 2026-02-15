/// <summary>
/// Input state for the car physics computation each frame.
/// </summary>
public struct CarInput
{
    public bool Throttle;
    public bool Brake;
    public bool SteerLeft;
    public bool SteerRight;
}

/// <summary>
/// Per-wheel ray data gathered from Godot raycasts.
/// </summary>
public struct WheelRayData
{
    public SpringInput Spring;
    /// <summary>Surface normal at hit point (unit vector, only valid if Spring.Hit).</summary>
    public float NormalX, NormalY, NormalZ;
}

/// <summary>
/// Per-wheel output from the physics computation.
/// </summary>
public struct WheelResult
{
    /// <summary>Force vector to apply at the wheel attachment point (world space).</summary>
    public float ForceX, ForceY, ForceZ;
    /// <summary>Wheel visual offset below the attachment point.</summary>
    public float WheelOffset;
    /// <summary>Compression ratio 0..1.</summary>
    public float Compression;
    /// <summary>Whether this wheel is grounded.</summary>
    public bool Grounded;
}

/// <summary>
/// Full output of the car physics computation for one frame.
/// </summary>
public struct CarPhysicsResult
{
    public WheelResult Wheel0, Wheel1, Wheel2, Wheel3;
    /// <summary>Drive force vector to apply at the chassis center (world space).</summary>
    public float DriveForceX, DriveForceY, DriveForceZ;
    /// <summary>Target angular velocity around Y axis (rad/s) from bicycle model.</summary>
    public float SteeringYawSpeed;
    /// <summary>Current front wheel steering angle (rad, positive = left).</summary>
    public float SteerAngle;
    /// <summary>Number of wheels currently grounded.</summary>
    public int GroundedCount;
}

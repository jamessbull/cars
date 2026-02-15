using System;

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
    /// <summary>Steering torque around Y axis.</summary>
    public float SteeringTorqueY;
    /// <summary>Number of wheels currently grounded.</summary>
    public int GroundedCount;
}

/// <summary>
/// Orchestrates 4 suspension springs + drive/steering forces. Pure C# — no Godot dependency.
/// </summary>
public static class CarPhysicsLogic
{
    /// <summary>
    /// Compute all forces for one physics frame.
    /// </summary>
    /// <param name="input">Player input state.</param>
    /// <param name="rays">Ray data for wheels 0-3 (FL, FR, RL, RR).</param>
    /// <param name="forwardX">Chassis forward direction X (unit vector).</param>
    /// <param name="forwardY">Chassis forward direction Y.</param>
    /// <param name="forwardZ">Chassis forward direction Z.</param>
    /// <param name="speed">Current speed along forward direction (positive = forward).</param>
    public static CarPhysicsResult ComputePhysics(
        CarInput input,
        WheelRayData[] rays,
        float forwardX, float forwardY, float forwardZ,
        float speed)
    {
        if (rays == null || rays.Length < 4)
            throw new ArgumentException("Exactly 4 wheel rays required", nameof(rays));

        var result = new CarPhysicsResult();

        // Compute each wheel's suspension
        var w0 = ComputeWheel(rays[0]);
        var w1 = ComputeWheel(rays[1]);
        var w2 = ComputeWheel(rays[2]);
        var w3 = ComputeWheel(rays[3]);

        result.Wheel0 = w0;
        result.Wheel1 = w1;
        result.Wheel2 = w2;
        result.Wheel3 = w3;

        int grounded = (w0.Grounded ? 1 : 0) + (w1.Grounded ? 1 : 0) +
                       (w2.Grounded ? 1 : 0) + (w3.Grounded ? 1 : 0);
        result.GroundedCount = grounded;

        // Drive forces — only when at least one wheel is grounded
        if (grounded > 0)
        {
            float driveForce = 0f;

            if (input.Throttle)
            {
                driveForce = CarConstants.ThrottleForce;
            }
            else if (input.Brake)
            {
                if (speed > 0.5f)
                {
                    // Braking: oppose forward velocity
                    driveForce = -CarConstants.BrakeForce;
                }
                else
                {
                    // Reverse at half power
                    driveForce = -CarConstants.ReverseForce;
                }
            }

            result.DriveForceX = forwardX * driveForce;
            result.DriveForceY = forwardY * driveForce;
            result.DriveForceZ = forwardZ * driveForce;

            // Steering torque
            float steer = 0f;
            if (input.SteerLeft)
                steer += CarConstants.SteeringTorque;
            if (input.SteerRight)
                steer -= CarConstants.SteeringTorque;
            result.SteeringTorqueY = steer;
        }

        return result;
    }

    private static WheelResult ComputeWheel(WheelRayData ray)
    {
        var spring = SuspensionSpring.ComputeForce(
            ray.Spring,
            CarConstants.SpringRestLength,
            CarConstants.SpringStiffness,
            CarConstants.SpringDamping,
            CarConstants.MaxSpringForce,
            CarConstants.WheelRadius);

        var result = new WheelResult();
        result.WheelOffset = spring.WheelOffset;
        result.Compression = spring.Compression;
        result.Grounded = spring.Grounded;

        if (spring.Grounded)
        {
            // Apply force along surface normal
            result.ForceX = ray.NormalX * spring.Force;
            result.ForceY = ray.NormalY * spring.Force;
            result.ForceZ = ray.NormalZ * spring.Force;
        }

        return result;
    }
}

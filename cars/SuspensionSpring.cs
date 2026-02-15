using System;

/// <summary>
/// Input data for a single suspension ray.
/// </summary>
public struct SpringInput
{
    /// <summary>Whether the ray hit anything.</summary>
    public bool Hit;
    /// <summary>Distance from ray origin to hit point (only valid if Hit is true).</summary>
    public float HitDistance;
    /// <summary>Velocity of the chassis at the wheel attachment point, projected onto the ray direction (downward positive).</summary>
    public float VerticalVelocity;
}

/// <summary>
/// Output from a single suspension spring computation.
/// </summary>
public struct SpringResult
{
    /// <summary>Upward force magnitude to apply at the wheel attachment point.</summary>
    public float Force;
    /// <summary>Compression ratio 0..1 (0 = fully extended, 1 = fully compressed).</summary>
    public float Compression;
    /// <summary>Vertical offset for wheel visual from the attachment point (negative = below chassis).</summary>
    public float WheelOffset;
    /// <summary>Whether this wheel is in contact with the ground.</summary>
    public bool Grounded;
}

/// <summary>
/// Computes suspension spring force for a single wheel. Pure C# — no Godot dependency.
/// F = kx + cv, clamped to push-only [0, maxForce].
/// </summary>
public static class SuspensionSpring
{
    public static SpringResult ComputeForce(
        SpringInput input,
        float restLength,
        float stiffness,
        float damping,
        float maxForce,
        float wheelRadius)
    {
        var result = new SpringResult();

        if (!input.Hit)
        {
            // No ground contact — wheel hangs at full extension
            result.Force = 0f;
            result.Compression = 0f;
            result.WheelOffset = -(restLength + wheelRadius);
            result.Grounded = false;
            return result;
        }

        // Compression: how much shorter the spring is compared to rest length
        // hitDistance is from chassis attachment point downward to surface
        // Spring length = hitDistance - wheelRadius (spring sits above the wheel)
        float springLength = input.HitDistance - wheelRadius;
        float compression = restLength - springLength;

        // Spring force: F = kx + cv (damping opposes velocity)
        // Positive verticalVelocity = moving downward = compressing = should add force
        float force = stiffness * compression + damping * input.VerticalVelocity;

        // Push-only: spring can only push chassis up, never pull it down
        force = Math.Max(0f, force);

        // Clamp to max force
        force = Math.Min(force, maxForce);

        result.Force = force;
        result.Compression = Math.Clamp(compression / restLength, 0f, 1f);
        result.WheelOffset = -(input.HitDistance - wheelRadius);
        result.Grounded = true;

        return result;
    }
}

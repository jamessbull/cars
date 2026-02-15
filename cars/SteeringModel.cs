using System;

/// <summary>
/// Output from steering computation for one frame.
/// </summary>
public struct SteeringResult
{
    /// <summary>Updated front wheel angle (rad, positive = left).</summary>
    public float SteerAngle;
    /// <summary>Target angular velocity around Y axis (rad/s) from bicycle model.</summary>
    public float YawSpeed;
}

/// <summary>
/// Bicycle-model steering: wheel angle ramps toward input, yaw rate derived from
/// speed and geometry. Pure C# — no Godot dependency.
/// </summary>
public static class SteeringModel
{
    /// <summary>
    /// Update steering angle and compute yaw rate.
    /// </summary>
    /// <param name="steerLeft">Left input held.</param>
    /// <param name="steerRight">Right input held.</param>
    /// <param name="currentAngle">Current front wheel angle (rad).</param>
    /// <param name="speed">Forward speed (positive = forward).</param>
    /// <param name="grounded">Whether at least one wheel is grounded.</param>
    public static SteeringResult Compute(
        bool steerLeft, bool steerRight,
        float currentAngle, float speed, bool grounded)
    {
        // Target angle from input
        float targetAngle = 0f;
        if (steerLeft)
            targetAngle += CarConstants.MaxSteerAngle;
        if (steerRight)
            targetAngle -= CarConstants.MaxSteerAngle;

        // Ramp toward target one step per frame
        float angle = currentAngle;
        if (targetAngle > angle)
            angle = Math.Min(angle + CarConstants.SteerAngleStep, targetAngle);
        else if (targetAngle < angle)
            angle = Math.Max(angle - CarConstants.SteerAngleStep, targetAngle);

        var result = new SteeringResult();
        result.SteerAngle = angle;

        // Bicycle model: yaw_rate = speed * tan(angle) / wheelbase
        // Only produces yaw when grounded
        if (grounded)
            result.YawSpeed = (speed * (float)Math.Tan(angle)) / CarConstants.Wheelbase;

        return result;
    }
}

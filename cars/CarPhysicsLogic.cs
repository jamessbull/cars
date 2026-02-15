using System;

/// <summary>
/// Orchestrates suspension, steering, and drive forces for one physics frame.
/// Pure C# — no Godot dependency.
/// </summary>
public static class CarPhysicsLogic
{
    public static CarPhysicsResult ComputePhysics(
        CarInput input,
        WheelRayData[] rays,
        float forwardX, float forwardY, float forwardZ,
        float speed,
        float currentSteerAngle)
    {
        if (rays == null || rays.Length < 4)
            throw new ArgumentException("Exactly 4 wheel rays required", nameof(rays));

        var result = new CarPhysicsResult();

        // Suspension
        result.Wheel0 = ComputeWheel(rays[0]);
        result.Wheel1 = ComputeWheel(rays[1]);
        result.Wheel2 = ComputeWheel(rays[2]);
        result.Wheel3 = ComputeWheel(rays[3]);

        int grounded = (result.Wheel0.Grounded ? 1 : 0) + (result.Wheel1.Grounded ? 1 : 0) +
                       (result.Wheel2.Grounded ? 1 : 0) + (result.Wheel3.Grounded ? 1 : 0);
        result.GroundedCount = grounded;

        // Steering
        var steering = SteeringModel.Compute(
            input.SteerLeft, input.SteerRight,
            currentSteerAngle, speed, grounded > 0);
        result.SteerAngle = steering.SteerAngle;
        result.SteeringYawSpeed = steering.YawSpeed;

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
                    driveForce = -CarConstants.BrakeForce;
                else
                    driveForce = -CarConstants.ReverseForce;
            }

            result.DriveForceX = forwardX * driveForce;
            result.DriveForceY = forwardY * driveForce;
            result.DriveForceZ = forwardZ * driveForce;
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
            result.ForceX = 0f;
            result.ForceY = spring.Force;
            result.ForceZ = 0f;
        }

        return result;
    }
}

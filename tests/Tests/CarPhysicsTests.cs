namespace Tests;

public class SuspensionSpringTests
{
    private const float Rest = CarConstants.SpringRestLength;      // 0.5
    private const float K = CarConstants.SpringStiffness;           // 1200
    private const float C = CarConstants.SpringDamping;             // 75
    private const float MaxForce = CarConstants.MaxSpringForce;     // 2000
    private const float WheelR = CarConstants.WheelRadius;          // 0.25

    private static SpringResult Compute(SpringInput input) =>
        SuspensionSpring.ComputeForce(input, Rest, K, C, MaxForce, WheelR);

    [Fact]
    public void NoHit_ZeroForce()
    {
        var input = new SpringInput { Hit = false };
        var result = Compute(input);

        Assert.Equal(0f, result.Force);
        Assert.False(result.Grounded);
        Assert.Equal(0f, result.Compression);
    }

    [Fact]
    public void NoHit_WheelHangsAtFullExtension()
    {
        var input = new SpringInput { Hit = false };
        var result = Compute(input);

        Assert.Equal(-(Rest + WheelR), result.WheelOffset);
    }

    [Fact]
    public void HitAtRestLength_ZeroVelocity_ZeroForce()
    {
        // Spring at exactly rest length: compression = 0 → force = 0
        var input = new SpringInput
        {
            Hit = true,
            HitDistance = Rest + WheelR, // spring length = rest
            VerticalVelocity = 0f
        };
        var result = Compute(input);

        Assert.Equal(0f, result.Force, 0.01f);
        Assert.True(result.Grounded);
    }

    [Fact]
    public void Compression_ProducesPositiveForce()
    {
        // Hit closer than rest → compressed → positive force
        float hitDist = 0.4f + WheelR; // spring length = 0.4, compression = 0.1
        var input = new SpringInput
        {
            Hit = true,
            HitDistance = hitDist,
            VerticalVelocity = 0f
        };
        var result = Compute(input);

        float expectedForce = K * 0.1f; // 80 N
        Assert.Equal(expectedForce, result.Force, 0.01f);
        Assert.True(result.Grounded);
    }

    [Fact]
    public void Compression_ProportionalToCompression()
    {
        // Double the compression → double the force (with zero velocity)
        float hitSmall = (Rest - 0.1f) + WheelR; // compression = 0.1
        float hitBig = (Rest - 0.2f) + WheelR;   // compression = 0.2

        var r1 = Compute(new SpringInput { Hit = true, HitDistance = hitSmall, VerticalVelocity = 0f });
        var r2 = Compute(new SpringInput { Hit = true, HitDistance = hitBig, VerticalVelocity = 0f });

        Assert.Equal(r1.Force * 2f, r2.Force, 0.01f);
    }

    [Fact]
    public void DampingAddsForce_WhenMovingDown()
    {
        float hitDist = Rest + WheelR; // at rest length, no spring force
        var input = new SpringInput
        {
            Hit = true,
            HitDistance = hitDist,
            VerticalVelocity = 1.0f // moving down
        };
        var result = Compute(input);

        Assert.Equal(C * 1.0f, result.Force, 0.01f); // 60 N from damping alone
    }

    [Fact]
    public void DampingReducesForce_WhenMovingUp()
    {
        // High compression so spring force exceeds damping at moderate extension speed
        float hitDist = (Rest - 0.4f) + WheelR; // compression = 0.4
        var input = new SpringInput
        {
            Hit = true,
            HitDistance = hitDist,
            VerticalVelocity = -0.5f // moving up (extending) slowly
        };
        var result = Compute(input);

        float withoutDamping = K * 0.4f;
        float expected = K * 0.4f + C * (-0.5f);
        Assert.True(expected > 0, "Test setup: spring must exceed damping");
        Assert.Equal(expected, result.Force, 0.01f);
        Assert.True(result.Force < withoutDamping); // damping reduced the force
    }

    [Fact]
    public void PushOnly_NegativeForceClampedToZero()
    {
        // Extended beyond rest length → negative spring force → clamped to 0
        float hitDist = (Rest + 0.1f) + WheelR; // spring longer than rest
        var input = new SpringInput
        {
            Hit = true,
            HitDistance = hitDist,
            VerticalVelocity = 0f
        };
        var result = Compute(input);

        Assert.Equal(0f, result.Force);
        Assert.True(result.Grounded); // still grounded even if force is zero
    }

    [Fact]
    public void MaxForceClamp()
    {
        // Extreme compression + huge downward velocity
        var input = new SpringInput
        {
            Hit = true,
            HitDistance = WheelR, // spring length = 0
            VerticalVelocity = 100f // huge downward velocity
        };
        var result = Compute(input);

        // kx + cv = 1200*0.5 + 75*100 = 600 + 7500 = 8100 → clamped to 2000
        Assert.Equal(MaxForce, result.Force);
    }

    [Fact]
    public void WheelOffset_MatchesHitDistance()
    {
        float hitDist = 0.4f + WheelR;
        var input = new SpringInput
        {
            Hit = true,
            HitDistance = hitDist,
            VerticalVelocity = 0f
        };
        var result = Compute(input);

        Assert.Equal(-0.4f, result.WheelOffset, 0.001f);
    }

    [Fact]
    public void CompressionRatio_Clamped01()
    {
        // Fully compressed
        var input = new SpringInput
        {
            Hit = true,
            HitDistance = WheelR, // spring length = 0
            VerticalVelocity = 0f
        };
        var result = Compute(input);

        Assert.Equal(1.0f, result.Compression);
    }
}

public class CarPhysicsLogicTests
{
    private static WheelRayData GroundedWheel(float hitDist = 0.5f, float velocity = 0f) =>
        new WheelRayData
        {
            Spring = new SpringInput { Hit = true, HitDistance = hitDist, VerticalVelocity = velocity },
            NormalX = 0f, NormalY = 1f, NormalZ = 0f
        };

    private static WheelRayData AirborneWheel() =>
        new WheelRayData
        {
            Spring = new SpringInput { Hit = false }
        };

    private static WheelRayData[] AllGrounded(float hitDist = 0.5f) =>
        new[] { GroundedWheel(hitDist), GroundedWheel(hitDist), GroundedWheel(hitDist), GroundedWheel(hitDist) };

    private static WheelRayData[] AllAirborne() =>
        new[] { AirborneWheel(), AirborneWheel(), AirborneWheel(), AirborneWheel() };

    [Fact]
    public void Throttle_ProducesForwardForce()
    {
        var input = new CarInput { Throttle = true };
        var result = CarPhysicsLogic.ComputePhysics(input, AllGrounded(), 0, 0, -1, 0f);

        Assert.Equal(0f, result.DriveForceX);
        Assert.True(result.DriveForceZ < 0); // forward is -Z
        Assert.Equal(-CarConstants.ThrottleForce, result.DriveForceZ, 0.01f);
    }

    [Fact]
    public void Brake_WhenMoving_OpposeVelocity()
    {
        var input = new CarInput { Brake = true };
        var result = CarPhysicsLogic.ComputePhysics(input, AllGrounded(), 0, 0, -1, 5.0f);

        // Brake should push backward (positive Z when forward is -Z)
        Assert.True(result.DriveForceZ > 0);
        Assert.Equal(CarConstants.BrakeForce, result.DriveForceZ, 0.01f);
    }

    [Fact]
    public void Brake_WhenStopped_Reverse()
    {
        var input = new CarInput { Brake = true };
        var result = CarPhysicsLogic.ComputePhysics(input, AllGrounded(), 0, 0, -1, 0f);

        // Should reverse (same direction as forward but negative = backward along forward)
        Assert.Equal(CarConstants.ReverseForce, result.DriveForceZ, 0.01f);
    }

    [Fact]
    public void SteerLeft_PositiveTorque()
    {
        var input = new CarInput { SteerLeft = true };
        var result = CarPhysicsLogic.ComputePhysics(input, AllGrounded(), 0, 0, -1, 5f);

        Assert.Equal(CarConstants.SteeringSpeed, result.SteeringYawSpeed, 0.01f);
    }

    [Fact]
    public void SteerRight_NegativeTorque()
    {
        var input = new CarInput { SteerRight = true };
        var result = CarPhysicsLogic.ComputePhysics(input, AllGrounded(), 0, 0, -1, 5f);

        Assert.Equal(-CarConstants.SteeringSpeed, result.SteeringYawSpeed, 0.01f);
    }

    [Fact]
    public void Steering_OnlyWhenGrounded()
    {
        var input = new CarInput { SteerLeft = true };
        var result = CarPhysicsLogic.ComputePhysics(input, AllAirborne(), 0, 0, -1, 5f);

        Assert.Equal(0f, result.SteeringYawSpeed);
    }

    [Fact]
    public void AllAirborne_NoDriveForce()
    {
        var input = new CarInput { Throttle = true };
        var result = CarPhysicsLogic.ComputePhysics(input, AllAirborne(), 0, 0, -1, 0f);

        Assert.Equal(0f, result.DriveForceX);
        Assert.Equal(0f, result.DriveForceY);
        Assert.Equal(0f, result.DriveForceZ);
        Assert.Equal(0, result.GroundedCount);
    }

    [Fact]
    public void AllAirborne_NoSuspensionForce()
    {
        var input = new CarInput();
        var result = CarPhysicsLogic.ComputePhysics(input, AllAirborne(), 0, 0, -1, 0f);

        Assert.Equal(0f, result.Wheel0.ForceX);
        Assert.Equal(0f, result.Wheel0.ForceY);
        Assert.Equal(0f, result.Wheel0.ForceZ);
    }

    [Fact]
    public void GroundedCount_Correct()
    {
        var rays = new[] { GroundedWheel(), GroundedWheel(), AirborneWheel(), AirborneWheel() };
        var input = new CarInput();
        var result = CarPhysicsLogic.ComputePhysics(input, rays, 0, 0, -1, 0f);

        Assert.Equal(2, result.GroundedCount);
    }

    [Fact]
    public void IndependentWheelForces()
    {
        // Different hit distances → different forces
        var rays = new[]
        {
            GroundedWheel(0.4f), // more compressed
            GroundedWheel(0.6f), // less compressed
            GroundedWheel(0.4f),
            GroundedWheel(0.6f)
        };
        var input = new CarInput();
        var result = CarPhysicsLogic.ComputePhysics(input, rays, 0, 0, -1, 0f);

        // Wheel 0 (hit 0.4) should have more force than Wheel 1 (hit 0.6)
        Assert.True(result.Wheel0.ForceY > result.Wheel1.ForceY);
    }

    [Fact]
    public void SuspensionForce_AlongSpringAxis_NotSurfaceNormal()
    {
        // Tilted surface normal should NOT affect force direction
        // Spring always pushes along local +Y (car's up axis)
        float nx = 0.0f, ny = 0.866f, nz = 0.5f; // ~30 degree slope
        var ray = new WheelRayData
        {
            Spring = new SpringInput { Hit = true, HitDistance = 0.4f, VerticalVelocity = 0f },
            NormalX = nx, NormalY = ny, NormalZ = nz
        };
        var rays = new[] { ray, GroundedWheel(), GroundedWheel(), GroundedWheel() };
        var input = new CarInput();
        var result = CarPhysicsLogic.ComputePhysics(input, rays, 0, 0, -1, 0f);

        // Force is purely along Y regardless of surface normal
        Assert.Equal(0f, result.Wheel0.ForceX);
        Assert.Equal(0f, result.Wheel0.ForceZ);
        Assert.True(result.Wheel0.ForceY > 0);
    }

    [Fact]
    public void NoInput_NoDriveForce()
    {
        var input = new CarInput();
        var result = CarPhysicsLogic.ComputePhysics(input, AllGrounded(), 0, 0, -1, 5f);

        Assert.Equal(0f, result.DriveForceX);
        Assert.Equal(0f, result.DriveForceY);
        Assert.Equal(0f, result.DriveForceZ);
        Assert.Equal(0f, result.SteeringYawSpeed);
    }

    [Fact]
    public void PartiallyGrounded_StillHasDriveForce()
    {
        var rays = new[] { GroundedWheel(), AirborneWheel(), AirborneWheel(), AirborneWheel() };
        var input = new CarInput { Throttle = true };
        var result = CarPhysicsLogic.ComputePhysics(input, rays, 1, 0, 0, 0f);

        Assert.Equal(CarConstants.ThrottleForce, result.DriveForceX, 0.01f);
        Assert.Equal(1, result.GroundedCount);
    }
}

using Godot;

public partial class CarBody : RigidBody3D
{
    private RayCast3D[] _rays = new RayCast3D[4];
    private MeshInstance3D[] _wheels = new MeshInstance3D[4];

    // Wheel attachment points relative to chassis center
    private static readonly Vector3[] WheelPositions = new Vector3[]
    {
        new Vector3(-CarConstants.TrackHalf, 0, -CarConstants.WheelBaseHalf), // FL
        new Vector3(CarConstants.TrackHalf, 0, -CarConstants.WheelBaseHalf),  // FR
        new Vector3(-CarConstants.TrackHalf, 0, CarConstants.WheelBaseHalf),  // RL
        new Vector3(CarConstants.TrackHalf, 0, CarConstants.WheelBaseHalf),   // RR
    };

    public override void _Ready()
    {
        Mass = CarConstants.ChassisMass;

        for (int i = 0; i < 4; i++)
        {
            _rays[i] = GetNode<RayCast3D>($"Ray{i}");
            _wheels[i] = GetNode<MeshInstance3D>($"Wheel{i}");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = new CarInput
        {
            Throttle = Input.IsActionPressed("ui_up"),
            Brake = Input.IsActionPressed("ui_down"),
            SteerLeft = Input.IsActionPressed("ui_left"),
            SteerRight = Input.IsActionPressed("ui_right")
        };

        var rays = new WheelRayData[4];
        for (int i = 0; i < 4; i++)
        {
            rays[i] = GatherRayData(i);
        }

        var forward = -GlobalTransform.Basis.Z;
        float speed = LinearVelocity.Dot(forward);

        var result = CarPhysicsLogic.ComputePhysics(
            input, rays,
            forward.X, forward.Y, forward.Z,
            speed);

        // Apply suspension forces at wheel attachment points
        ApplyWheelForce(result.Wheel0, 0);
        ApplyWheelForce(result.Wheel1, 1);
        ApplyWheelForce(result.Wheel2, 2);
        ApplyWheelForce(result.Wheel3, 3);

        // Apply drive force at center of mass
        var driveForce = new Vector3(result.DriveForceX, result.DriveForceY, result.DriveForceZ);
        ApplyCentralForce(driveForce);

        // Apply steering torque
        if (result.SteeringTorqueY != 0f)
        {
            ApplyTorque(new Vector3(0, result.SteeringTorqueY, 0));
        }

        // Position wheel meshes
        PositionWheels(result);
    }

    private WheelRayData GatherRayData(int index)
    {
        var ray = _rays[index];
        var data = new WheelRayData();

        if (ray.IsColliding())
        {
            var origin = ray.GlobalPosition;
            var hitPoint = ray.GetCollisionPoint();
            var hitNormal = ray.GetCollisionNormal();
            float hitDist = origin.DistanceTo(hitPoint);

            // Compute vertical velocity at this attachment point
            var attachLocal = WheelPositions[index];
            var velocity = GetVelocityAtLocalPoint(attachLocal);
            var downDir = -GlobalTransform.Basis.Y;
            float vertVel = velocity.Dot(downDir);

            data.Spring = new SpringInput
            {
                Hit = true,
                HitDistance = hitDist,
                VerticalVelocity = vertVel
            };
            data.NormalX = hitNormal.X;
            data.NormalY = hitNormal.Y;
            data.NormalZ = hitNormal.Z;
        }
        else
        {
            data.Spring = new SpringInput { Hit = false };
        }

        return data;
    }

    private Vector3 GetVelocityAtLocalPoint(Vector3 localPoint)
    {
        var worldOffset = GlobalTransform.Basis * localPoint;
        return LinearVelocity + AngularVelocity.Cross(worldOffset);
    }

    private void ApplyWheelForce(WheelResult wheel, int index)
    {
        if (!wheel.Grounded) return;

        var force = new Vector3(wheel.ForceX, wheel.ForceY, wheel.ForceZ);
        var attachWorld = GlobalTransform * WheelPositions[index];
        var offset = attachWorld - GlobalPosition;
        ApplyForce(force, offset);
    }

    private void PositionWheels(CarPhysicsResult result)
    {
        SetWheelPos(0, result.Wheel0);
        SetWheelPos(1, result.Wheel1);
        SetWheelPos(2, result.Wheel2);
        SetWheelPos(3, result.Wheel3);
    }

    private void SetWheelPos(int index, WheelResult wheel)
    {
        var basePos = WheelPositions[index];
        _wheels[index].Position = new Vector3(basePos.X, wheel.WheelOffset, basePos.Z);
    }
}

using Godot;

public partial class CarBody : RigidBody3D
{
    private MeshInstance3D[] _wheels = new MeshInstance3D[4];
    private float _steerAngle; // current front wheel angle (rad, positive = left)
    private GridMap _gridMap;

    // Wheel attachment points relative to chassis center
    private static readonly Vector3[] WheelPositions = new Vector3[]
    {
        new Vector3(-CarConstants.TrackHalf, 0, -CarConstants.WheelBaseHalf), // FL
        new Vector3(CarConstants.TrackHalf, 0, -CarConstants.WheelBaseHalf),  // FR
        new Vector3(-CarConstants.TrackHalf, 0, CarConstants.WheelBaseHalf),  // RL
        new Vector3(CarConstants.TrackHalf, 0, CarConstants.WheelBaseHalf),   // RR
    };

    // How far above each wheel attachment to start the ray (must be above any terrain)
    private const float RaycastCeilingOffset = 10f;
    // Maximum angular velocity on pitch/roll axes (rad/s) — prevents wild spinning
    private const float MaxPitchRollSpeed = 3f;
    // Angular damping on pitch/roll per frame — bleeds off spin
    private const float PitchRollDamping = 0.9f;

    public override void _Ready()
    {
        Mass = CarConstants.ChassisMass;
        ContinuousCd = true; // prevent tunneling through walls at high speed

        for (int i = 0; i < 4; i++)
        {
            _wheels[i] = GetNode<MeshInstance3D>($"Wheel{i}");
        }

        // Cache GridMap reference (TrackGridMap sibling's child)
        var trackGridMap = GetParent().GetNodeOrNull<Node3D>("TrackGridMap");
        if (trackGridMap != null)
        {
            foreach (var child in trackGridMap.GetChildren())
            {
                if (child is GridMap gm)
                {
                    _gridMap = gm;
                    break;
                }
            }
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

        var spaceState = GetWorld3D().DirectSpaceState;
        var rays = new WheelRayData[4];
        for (int i = 0; i < 4; i++)
        {
            rays[i] = GatherRayData(i, spaceState);
        }

        var forward = -GlobalTransform.Basis.Z;
        float speed = LinearVelocity.Dot(forward);

        var result = CarPhysicsLogic.ComputePhysics(
            input, rays,
            forward.X, forward.Y, forward.Z,
            speed, _steerAngle);

        _steerAngle = result.SteerAngle;

        // Apply suspension forces at wheel attachment points — always world-up
        ApplyWheelForce(result.Wheel0, 0);
        ApplyWheelForce(result.Wheel1, 1);
        ApplyWheelForce(result.Wheel2, 2);
        ApplyWheelForce(result.Wheel3, 3);

        // Apply drive force at center of mass
        var driveForce = new Vector3(result.DriveForceX, result.DriveForceY, result.DriveForceZ);
        ApplyCentralForce(driveForce);

        // Kill lateral velocity — with perfect grip, the car only moves
        // in the direction it's facing (no sideways drift)
        // Use flat (XZ) forward to avoid double-counting vertical velocity on ramps
        if (result.GroundedCount > 0)
        {
            var flatForward = new Vector3(forward.X, 0f, forward.Z).Normalized();
            float flatSpeed = LinearVelocity.Dot(flatForward);
            float verticalSpeed = LinearVelocity.Y;
            LinearVelocity = flatForward * flatSpeed + Vector3.Up * verticalSpeed;

            // Rolling resistance — always applies when grounded
            LinearVelocity *= CarConstants.RollingDragFactor;

            // Apply extra gravel drag if on a gravel tile
            if (_gridMap != null)
            {
                var mapPos = _gridMap.LocalToMap(_gridMap.ToLocal(GlobalPosition));
                int cellItem = _gridMap.GetCellItem(mapPos);
                if (cellItem == (int)TileType.Gravel)
                {
                    LinearVelocity *= CarConstants.GravelDragFactor;
                }
            }
        }

        // Steering: directly set Y angular velocity (stops immediately on key release)
        // Also damp and clamp pitch/roll to prevent wild spinning
        var av = AngularVelocity;
        float dampedX = av.X * PitchRollDamping;
        float dampedZ = av.Z * PitchRollDamping;
        dampedX = Mathf.Clamp(dampedX, -MaxPitchRollSpeed, MaxPitchRollSpeed);
        dampedZ = Mathf.Clamp(dampedZ, -MaxPitchRollSpeed, MaxPitchRollSpeed);
        AngularVelocity = new Vector3(dampedX, result.SteeringYawSpeed, dampedZ);

        // Position wheel meshes
        PositionWheels(result);
    }

    private WheelRayData GatherRayData(int index, PhysicsDirectSpaceState3D spaceState)
    {
        var data = new WheelRayData();

        // Get wheel attachment point in world space
        var attachWorld = GlobalTransform * WheelPositions[index];

        // Cast ray from well above the wheel straight down in world space
        var from = new Vector3(attachWorld.X, attachWorld.Y + RaycastCeilingOffset, attachWorld.Z);
        var to = new Vector3(attachWorld.X, attachWorld.Y - RaycastCeilingOffset, attachWorld.Z);

        var query = PhysicsRayQueryParameters3D.Create(from, to, 1); // mask = layer 1 (track)
        query.HitFromInside = true;
        query.HitBackFaces = true;
        var hit = spaceState.IntersectRay(query);

        if (hit.Count > 0)
        {
            var hitPoint = (Vector3)hit["position"];
            var hitNormal = (Vector3)hit["normal"];

            // Vertical distance from wheel attachment point down to ground
            float hitDist = attachWorld.Y - hitPoint.Y;

            if (hitDist >= 0 && hitDist <= CarConstants.RayLength)
            {
                // Normal suspension range — compute damping using world-vertical velocity
                var velocity = GetVelocityAtLocalPoint(WheelPositions[index]);
                float vertVel = -velocity.Y; // positive = moving down in world space

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
                // Ground is beyond ray reach
                data.Spring = new SpringInput { Hit = false };
            }
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

        // Spring force is always WORLD UP — never rotated with the car.
        // This prevents positive feedback: tilt → horizontal force → more tilt.
        // Different compression at each wheel naturally pitches/rolls the car to match terrain.
        var worldForce = Vector3.Up * wheel.ForceY;
        var attachWorld = GlobalTransform * WheelPositions[index];
        var offset = attachWorld - GlobalPosition;
        ApplyForce(worldForce, offset);
    }

    private void PositionWheels(CarPhysicsResult result)
    {
        SetWheelPos(0, result.Wheel0, result.SteerAngle);
        SetWheelPos(1, result.Wheel1, result.SteerAngle);
        SetWheelPos(2, result.Wheel2, 0f);
        SetWheelPos(3, result.Wheel3, 0f);
    }

    private void SetWheelPos(int index, WheelResult wheel, float steerAngle)
    {
        var basePos = WheelPositions[index];
        _wheels[index].Position = new Vector3(basePos.X, wheel.WheelOffset, basePos.Z);

        // Rotate front wheels (0=FL, 1=FR) to show steering angle
        // Base rotation: 90° around Z to orient cylinder as a wheel (axis along X)
        // Steering: rotate around Y by steer angle
        if (index < 2)
        {
            var steerBasis = Basis.Identity.Rotated(Vector3.Up, steerAngle);
            var wheelBasis = new Basis(new Vector3(0, 1, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1));
            _wheels[index].Basis = steerBasis * wheelBasis;
        }
    }
}

using Godot;

public partial class CarCamera : Camera3D
{
    private Node3D _target;

    public override void _Ready()
    {
        _target = GetParent<Node3D>();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_target == null) return;

        var targetPos = _target.GlobalPosition;

        // Project the car's forward direction onto the XZ plane (ignore pitch/roll)
        var forward3D = -_target.GlobalTransform.Basis.Z;
        var flatForward = new Vector3(forward3D.X, 0f, forward3D.Z);
        if (flatForward.LengthSquared() < 0.001f)
            flatForward = new Vector3(0f, 0f, -1f); // fallback if pointing straight up/down
        flatForward = flatForward.Normalized();

        // Position camera behind (opposite of flat forward) and above
        var desiredPos = targetPos
            - flatForward * CarConstants.CameraBehind
            + Vector3.Up * CarConstants.CameraAbove;

        float t = (float)(1.0 - Mathf.Exp(-CarConstants.CameraSmoothing * delta));
        GlobalPosition = GlobalPosition.Lerp(desiredPos, t);
        LookAt(targetPos, Vector3.Up);
    }
}

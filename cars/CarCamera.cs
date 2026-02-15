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
        var back = _target.GlobalTransform.Basis.Z.Normalized();

        var desiredPos = targetPos
            + back * CarConstants.CameraBehind
            + Vector3.Up * CarConstants.CameraAbove;

        float t = (float)(1.0 - Mathf.Exp(-CarConstants.CameraSmoothing * delta));
        GlobalPosition = GlobalPosition.Lerp(desiredPos, t);
        LookAt(targetPos, Vector3.Up);
    }
}

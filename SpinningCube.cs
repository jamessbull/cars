using Godot;

public partial class SpinningCube : MeshInstance3D
{
	[Export] public float RotationSpeed = 2.0f;

	private CubeRotationLogic _logic;

	public override void _Ready()
	{
		_logic = new CubeRotationLogic(RotationSpeed);
	}

	public override void _Process(double delta)
	{
		_logic.RotationSpeed = RotationSpeed;

		var rotation = _logic.ComputeRotation(
			Input.IsActionPressed("ui_left"),
			Input.IsActionPressed("ui_right"),
			Input.IsActionPressed("ui_up"),
			Input.IsActionPressed("ui_down"),
			(float)delta
		);

		RotateY(rotation.RotateY);
		RotateX(rotation.RotateX);
	}
}

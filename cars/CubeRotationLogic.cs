public struct RotationResult
{
	public float RotateX;
	public float RotateY;
}

public class CubeRotationLogic
{
	public float RotationSpeed { get; set; }

	public CubeRotationLogic(float rotationSpeed = 2.0f)
	{
		RotationSpeed = rotationSpeed;
	}

	public RotationResult ComputeRotation(bool left, bool right, bool up, bool down, float delta)
	{
		float speed = RotationSpeed * delta;
		var result = new RotationResult();

		if (left)
			result.RotateY += speed;
		if (right)
			result.RotateY -= speed;
		if (up)
			result.RotateX -= speed;
		if (down)
			result.RotateX += speed;

		return result;
	}
}

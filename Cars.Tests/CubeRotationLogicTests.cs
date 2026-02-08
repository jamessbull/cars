using Xunit;

public class CubeRotationLogicTests
{
	private readonly CubeRotationLogic _logic = new(rotationSpeed: 2.0f);
	private const float Delta = 0.016f; // ~60fps

	[Fact]
	public void NoInput_ReturnsZeroRotation()
	{
		var result = _logic.ComputeRotation(false, false, false, false, Delta);

		Assert.Equal(0f, result.RotateX);
		Assert.Equal(0f, result.RotateY);
	}

	[Fact]
	public void LeftPressed_RotatesYPositive()
	{
		var result = _logic.ComputeRotation(left: true, right: false, up: false, down: false, Delta);

		Assert.True(result.RotateY > 0);
		Assert.Equal(0f, result.RotateX);
	}

	[Fact]
	public void RightPressed_RotatesYNegative()
	{
		var result = _logic.ComputeRotation(left: false, right: true, up: false, down: false, Delta);

		Assert.True(result.RotateY < 0);
		Assert.Equal(0f, result.RotateX);
	}

	[Fact]
	public void UpPressed_RotatesXNegative()
	{
		var result = _logic.ComputeRotation(left: false, right: false, up: true, down: false, Delta);

		Assert.Equal(0f, result.RotateY);
		Assert.True(result.RotateX < 0);
	}

	[Fact]
	public void DownPressed_RotatesXPositive()
	{
		var result = _logic.ComputeRotation(left: false, right: false, up: false, down: true, Delta);

		Assert.Equal(0f, result.RotateY);
		Assert.True(result.RotateX > 0);
	}

	[Fact]
	public void LeftAndRight_CancelOut()
	{
		var result = _logic.ComputeRotation(left: true, right: true, up: false, down: false, Delta);

		Assert.Equal(0f, result.RotateY);
		Assert.Equal(0f, result.RotateX);
	}

	[Fact]
	public void UpAndDown_CancelOut()
	{
		var result = _logic.ComputeRotation(left: false, right: false, up: true, down: true, Delta);

		Assert.Equal(0f, result.RotateX);
		Assert.Equal(0f, result.RotateY);
	}

	[Fact]
	public void LeftAndUp_BothAxesRotate()
	{
		var result = _logic.ComputeRotation(left: true, right: false, up: true, down: false, Delta);

		Assert.True(result.RotateY > 0);
		Assert.True(result.RotateX < 0);
	}

	[Fact]
	public void DeltaScaling_LargerDelta_LargerRotation()
	{
		var small = _logic.ComputeRotation(left: true, right: false, up: false, down: false, 0.016f);
		var large = _logic.ComputeRotation(left: true, right: false, up: false, down: false, 0.032f);

		Assert.Equal(small.RotateY * 2, large.RotateY, precision: 5);
	}

	[Fact]
	public void DeltaScaling_ExactValue()
	{
		var result = _logic.ComputeRotation(left: true, right: false, up: false, down: false, 1.0f);

		Assert.Equal(2.0f, result.RotateY);
	}

	[Fact]
	public void ZeroDelta_ReturnsZeroRotation()
	{
		var result = _logic.ComputeRotation(left: true, right: false, up: true, down: false, 0f);

		Assert.Equal(0f, result.RotateX);
		Assert.Equal(0f, result.RotateY);
	}

	[Fact]
	public void CustomRotationSpeed_Applies()
	{
		var fastLogic = new CubeRotationLogic(rotationSpeed: 5.0f);
		var result = fastLogic.ComputeRotation(left: true, right: false, up: false, down: false, 1.0f);

		Assert.Equal(5.0f, result.RotateY);
	}
}

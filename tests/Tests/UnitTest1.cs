namespace Tests;

public class CubeRotationLogicTests
{
    private const float Delta = 1.0f;
    private const float DefaultSpeed = 2.0f;

    private readonly CubeRotationLogic _logic = new();

    [Fact]
    public void LeftKey_RotatesPositiveY()
    {
        var result = _logic.ComputeRotation(left: true, right: false, up: false, down: false, Delta);
        Assert.Equal(DefaultSpeed, result.RotateY);
        Assert.Equal(0f, result.RotateX);
    }

    [Fact]
    public void RightKey_RotatesNegativeY()
    {
        var result = _logic.ComputeRotation(left: false, right: true, up: false, down: false, Delta);
        Assert.Equal(-DefaultSpeed, result.RotateY);
        Assert.Equal(0f, result.RotateX);
    }

    [Fact]
    public void UpKey_RotatesNegativeX()
    {
        var result = _logic.ComputeRotation(left: false, right: false, up: true, down: false, Delta);
        Assert.Equal(0f, result.RotateY);
        Assert.Equal(-DefaultSpeed, result.RotateX);
    }

    [Fact]
    public void DownKey_RotatesPositiveX()
    {
        var result = _logic.ComputeRotation(left: false, right: false, up: false, down: true, Delta);
        Assert.Equal(0f, result.RotateY);
        Assert.Equal(DefaultSpeed, result.RotateX);
    }

    [Fact]
    public void NoInput_NoRotation()
    {
        var result = _logic.ComputeRotation(left: false, right: false, up: false, down: false, Delta);
        Assert.Equal(0f, result.RotateX);
        Assert.Equal(0f, result.RotateY);
    }

    [Fact]
    public void LeftAndRight_Cancel()
    {
        var result = _logic.ComputeRotation(left: true, right: true, up: false, down: false, Delta);
        Assert.Equal(0f, result.RotateY);
    }

    [Fact]
    public void UpAndDown_Cancel()
    {
        var result = _logic.ComputeRotation(left: false, right: false, up: true, down: true, Delta);
        Assert.Equal(0f, result.RotateX);
    }

    [Fact]
    public void MultiAxis_LeftAndUp()
    {
        var result = _logic.ComputeRotation(left: true, right: false, up: true, down: false, Delta);
        Assert.Equal(DefaultSpeed, result.RotateY);
        Assert.Equal(-DefaultSpeed, result.RotateX);
    }

    [Fact]
    public void MultiAxis_RightAndDown()
    {
        var result = _logic.ComputeRotation(left: false, right: true, up: false, down: true, Delta);
        Assert.Equal(-DefaultSpeed, result.RotateY);
        Assert.Equal(DefaultSpeed, result.RotateX);
    }

    [Fact]
    public void DeltaScaling_HalfDelta()
    {
        var result = _logic.ComputeRotation(left: true, right: false, up: false, down: false, 0.5f);
        Assert.Equal(DefaultSpeed * 0.5f, result.RotateY);
    }

    [Fact]
    public void ZeroDelta_NoRotation()
    {
        var result = _logic.ComputeRotation(left: true, right: false, up: true, down: false, 0f);
        Assert.Equal(0f, result.RotateX);
        Assert.Equal(0f, result.RotateY);
    }

    [Fact]
    public void CustomRotationSpeed()
    {
        var logic = new CubeRotationLogic(5.0f);
        var result = logic.ComputeRotation(left: true, right: false, up: false, down: false, Delta);
        Assert.Equal(5.0f, result.RotateY);
    }
}
